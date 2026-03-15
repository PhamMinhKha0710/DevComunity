using System.Text.Json;
using MongoDB.Driver;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Domain.Entities;
using SocialTechsy.SocialNetwork.Infrastructure.IdGeneration;
using SocialTechsy.SocialNetwork.Infrastructure.MongoDB.Models;
using SocialTechsy.SocialNetwork.Infrastructure.Redis;

namespace SocialTechsy.SocialNetwork.Infrastructure.MongoDB;

public class MongoChatRepository : IChatRepository
{
    private readonly MongoClient _client;
    private readonly IMongoCollection<ConversationDocument> _conversations;
    private readonly IMongoCollection<MessageDocument> _messages;
    private readonly IMongoCollection<CounterDocument> _counters;
    private readonly IMongoCollection<OutboxDocument> _outbox;
    private readonly IUserRepository _userRepository;
    private readonly RedisChatCacheService? _cache;
    private readonly SnowflakeIdGenerator _snowflake;

    private static bool _indexesCreated;
    private static readonly object _indexLock = new();

    public MongoChatRepository(
        MongoClient client,
        IMongoDatabase database,
        IUserRepository userRepository,
        SnowflakeIdGenerator snowflake,
        RedisChatCacheService? cache = null)
    {
        _client = client;
        _conversations = database.GetCollection<ConversationDocument>("conversations");
        _messages = database.GetCollection<MessageDocument>("messages");
        _counters = database.GetCollection<CounterDocument>("counters");
        _outbox = database.GetCollection<OutboxDocument>("outbox");
        _userRepository = userRepository;
        _snowflake = snowflake;
        _cache = cache;

        EnsureIndexes();
    }

    private void EnsureIndexes()
    {
        if (_indexesCreated) return;
        lock (_indexLock)
        {
            if (_indexesCreated) return;

            _messages.Indexes.CreateMany(new[]
            {
                new CreateIndexModel<MessageDocument>(
                    Builders<MessageDocument>.IndexKeys
                        .Ascending(m => m.ConversationId)
                        .Descending(m => m.MessageId)),
                new CreateIndexModel<MessageDocument>(
                    Builders<MessageDocument>.IndexKeys
                        .Ascending(m => m.ConversationId)
                        .Ascending(m => m.SentDate)),
                new CreateIndexModel<MessageDocument>(
                    Builders<MessageDocument>.IndexKeys
                        .Ascending(m => m.ConversationId)
                        .Ascending(m => m.IsRead)
                        .Ascending(m => m.SenderId))
            });

            _conversations.Indexes.CreateOne(new CreateIndexModel<ConversationDocument>(
                Builders<ConversationDocument>.IndexKeys.Ascending("Participants.UserId")));

            _indexesCreated = true;
        }
    }

    private async Task<int> GetNextSequenceAsync(string sequenceName)
    {
        if (_cache != null)
        {
            return checked((int)await _cache.GenerateIdAsync(sequenceName));
        }

        var filter = Builders<CounterDocument>.Filter.Eq(c => c.Id, sequenceName);
        var update = Builders<CounterDocument>.Update.Inc(c => c.SequenceValue, 1L);
        var options = new FindOneAndUpdateOptions<CounterDocument>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After
        };
        var counter = await _counters.FindOneAndUpdateAsync(filter, update, options);
        return checked((int)counter.SequenceValue);
    }

    private async Task<UserInfoEmbed> GetUserInfoAsync(int userId)
    {
        if (_cache != null)
        {
            var cached = await _cache.GetUserInfoAsync(userId);
            if (cached != null) return cached;
        }

        var user = await _userRepository.GetByIdAsync(userId);
        var info = new UserInfoEmbed
        {
            UserId = userId,
            Username = user?.Username ?? "Unknown",
            DisplayName = user?.DisplayName,
            ProfilePicture = user?.ProfilePicture
        };

        if (_cache != null)
        {
            await _cache.SetUserInfoAsync(info);
        }

        return info;
    }

    // ========== CONVERSATIONS ==========

    public async Task<Conversation?> GetConversationByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        if (_cache != null)
        {
            var cached = await _cache.GetConversationAsync(id);
            if (cached != null) return MapToConversation(cached);
        }

        var doc = await _conversations
            .Find(c => c.ConversationId == id)
            .FirstOrDefaultAsync(cancellationToken);

        if (doc == null) return null;

        if (_cache != null)
        {
            await _cache.SetConversationAsync(doc);
        }

        return MapToConversation(doc);
    }

    public async Task<(IEnumerable<Conversation> Items, int TotalCount)> GetUserConversationsAsync(
        int userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ConversationDocument>.Filter
            .ElemMatch(c => c.Participants, p => p.UserId == userId);
        var sort = Builders<ConversationDocument>.Sort.Descending(c => c.LastMessageDate);

        var totalCount = (int)await _conversations.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        var docs = await _conversations
            .Find(filter)
            .Sort(sort)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        var conversationIds = docs.Select(d => d.ConversationId).ToList();

        var lastMessages = await _messages.Aggregate()
            .Match(Builders<MessageDocument>.Filter.In(m => m.ConversationId, conversationIds))
            .SortByDescending(m => m.SentDate)
            .Group(m => m.ConversationId, g => new { ConversationId = g.Key, Doc = g.First() })
            .ToListAsync(cancellationToken);

        var lastMessageByConv = lastMessages.ToDictionary(x => x.ConversationId, x => x.Doc);

        var conversations = new List<Conversation>();
        foreach (var doc in docs)
        {
            var conv = MapToConversation(doc);

            if (lastMessageByConv.TryGetValue(doc.ConversationId, out var lastMsg))
            {
                conv.Messages = new List<Message> { MapToMessage(lastMsg) };
            }

            if (_cache != null)
            {
                await _cache.SetConversationAsync(doc);
            }

            conversations.Add(conv);
        }

        return (conversations, totalCount);
    }

    public async Task<Conversation?> GetConversationBetweenUsersAsync(
        int userId1, int userId2, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ConversationDocument>.Filter.And(
            Builders<ConversationDocument>.Filter.Size(c => c.Participants, 2),
            Builders<ConversationDocument>.Filter.ElemMatch(c => c.Participants, p => p.UserId == userId1),
            Builders<ConversationDocument>.Filter.ElemMatch(c => c.Participants, p => p.UserId == userId2)
        );

        var doc = await _conversations.Find(filter).FirstOrDefaultAsync(cancellationToken);
        return doc == null ? null : MapToConversation(doc);
    }

    public async Task<Conversation> CreateConversationAsync(Conversation conversation, CancellationToken cancellationToken = default)
    {
        var conversationId = await GetNextSequenceAsync("conversations");

        var participants = new List<ParticipantEmbed>();
        foreach (var p in conversation.Participants)
        {
            var participantId = await GetNextSequenceAsync("conversationParticipants");
            var userInfo = await GetUserInfoAsync(p.UserId);
            participants.Add(new ParticipantEmbed
            {
                ConversationParticipantId = participantId,
                UserId = p.UserId,
                JoinedDate = p.JoinedDate,
                LastReadDate = p.LastReadDate,
                User = userInfo
            });
        }

        var doc = new ConversationDocument
        {
            ConversationId = conversationId,
            Title = conversation.Title,
            IsGroupChat = conversation.IsGroupChat,
            ParticipantCount = participants.Count,
            GroupTier = ConversationDocument.DetermineGroupTier(participants.Count),
            CreatedDate = conversation.CreatedDate,
            LastMessageDate = conversation.LastMessageDate,
            Participants = participants
        };

        await _conversations.InsertOneAsync(doc, cancellationToken: cancellationToken);

        if (_cache != null)
        {
            await _cache.SetConversationAsync(doc);
        }

        conversation.ConversationId = conversationId;
        var participantsList = conversation.Participants.ToList();
        for (int i = 0; i < participantsList.Count; i++)
        {
            participantsList[i].ConversationParticipantId = participants[i].ConversationParticipantId;
            participantsList[i].ConversationId = conversationId;
        }

        return conversation;
    }

    // ========== MESSAGES ==========

    public async Task<Message?> GetMessageByIdAsync(long messageId, CancellationToken cancellationToken = default)
    {
        var doc = await _messages.Find(m => m.MessageId == messageId).FirstOrDefaultAsync(cancellationToken);
        if (doc == null) return null;

        var message = MapToMessage(doc);

        if (doc.ReplyToMessageId.HasValue)
        {
            var replyDoc = await _messages
                .Find(m => m.MessageId == doc.ReplyToMessageId.Value)
                .FirstOrDefaultAsync(cancellationToken);
            if (replyDoc != null)
            {
                message.ReplyToMessage = MapToMessage(replyDoc);
            }
        }

        return message;
    }

    public async Task<(IEnumerable<Message> Items, int TotalCount)> GetMessagesAsync(
        int conversationId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var filter = Builders<MessageDocument>.Filter.Eq(m => m.ConversationId, conversationId);
        var sort = Builders<MessageDocument>.Sort.Ascending(m => m.SentDate);

        var totalCount = (int)await _messages.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        var docs = await _messages
            .Find(filter)
            .Sort(sort)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        var replyIds = docs
            .Where(d => d.ReplyToMessageId.HasValue)
            .Select(d => d.ReplyToMessageId!.Value)
            .Distinct()
            .ToList();

        var replyDocs = new Dictionary<long, MessageDocument>();
        if (replyIds.Count > 0)
        {
            var replyFilter = Builders<MessageDocument>.Filter.In(m => m.MessageId, replyIds);
            var replies = await _messages.Find(replyFilter).ToListAsync(cancellationToken);
            replyDocs = replies.ToDictionary(r => r.MessageId);
        }

        var messages = docs.Select(doc =>
        {
            var msg = MapToMessage(doc);
            if (doc.ReplyToMessageId.HasValue &&
                replyDocs.TryGetValue(doc.ReplyToMessageId.Value, out var replyDoc))
            {
                msg.ReplyToMessage = MapToMessage(replyDoc);
            }
            return msg;
        }).ToList();

        return (messages, totalCount);
    }

    public async Task<IEnumerable<Message>> GetMessagesCursorAsync(
        int conversationId, long? afterMessageId, int limit, CancellationToken cancellationToken = default)
    {
        var filter = Builders<MessageDocument>.Filter.Eq(m => m.ConversationId, conversationId);
        if (afterMessageId.HasValue)
        {
            filter &= Builders<MessageDocument>.Filter.Gt(m => m.MessageId, afterMessageId.Value);
        }

        var docs = await _messages
            .Find(filter)
            .Sort(Builders<MessageDocument>.Sort.Ascending(m => m.MessageId))
            .Limit(limit)
            .ToListAsync(cancellationToken);

        var replyIds = docs
            .Where(d => d.ReplyToMessageId.HasValue)
            .Select(d => d.ReplyToMessageId!.Value)
            .Distinct()
            .ToList();

        var replyDocs = new Dictionary<long, MessageDocument>();
        if (replyIds.Count > 0)
        {
            var replyFilter = Builders<MessageDocument>.Filter.In(m => m.MessageId, replyIds);
            var replies = await _messages.Find(replyFilter).ToListAsync(cancellationToken);
            replyDocs = replies.ToDictionary(r => r.MessageId);
        }

        return docs.Select(doc =>
        {
            var msg = MapToMessage(doc);
            if (doc.ReplyToMessageId.HasValue &&
                replyDocs.TryGetValue(doc.ReplyToMessageId.Value, out var replyDoc))
            {
                msg.ReplyToMessage = MapToMessage(replyDoc);
            }
            return msg;
        }).ToList();
    }

    public async Task<Message> AddMessageAsync(Message message, CancellationToken cancellationToken = default)
    {
        var messageId = _snowflake.NextId();
        var senderInfo = await GetUserInfoAsync(message.SenderId);

        var doc = new MessageDocument
        {
            MessageId = messageId,
            ConversationId = message.ConversationId,
            SenderId = message.SenderId,
            Sender = senderInfo,
            Content = message.Content,
            IsRead = message.IsRead,
            SentDate = message.SentDate,
            MessageType = message.MessageType ?? "text",
            DeliveryStatus = (int)message.DeliveryStatus,
            AttachmentUrl = message.AttachmentUrl,
            AttachmentFileName = message.AttachmentFileName,
            AttachmentSize = message.AttachmentSize,
            ReplyToMessageId = message.ReplyToMessageId,
            Reactions = new List<ReactionEmbed>()
        };

        var convDoc = await _conversations
            .Find(c => c.ConversationId == message.ConversationId)
            .FirstOrDefaultAsync(cancellationToken);
        var participantIds = convDoc?.Participants.Select(p => p.UserId).ToList() ?? new List<int>();

        var outboxDoc = new OutboxDocument
        {
            EventType = ChatEventTypes.NewMessage,
            PayloadJson = JsonSerializer.Serialize(new NewMessagePayload
            {
                ConversationId = message.ConversationId,
                MessageId = messageId,
                SenderId = message.SenderId,
                SenderUsername = senderInfo.Username,
                ParticipantUserIds = participantIds
            }),
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            using var session = await _client.StartSessionAsync(cancellationToken: cancellationToken);
            session.StartTransaction();
            await _messages.InsertOneAsync(session, doc, cancellationToken: cancellationToken);
            await _outbox.InsertOneAsync(session, outboxDoc, cancellationToken: cancellationToken);
            var convFilter = Builders<ConversationDocument>.Filter.Eq(c => c.ConversationId, message.ConversationId);
            var convUpdate = Builders<ConversationDocument>.Update.Set(c => c.LastMessageDate, message.SentDate);
            await _conversations.UpdateOneAsync(session, convFilter, convUpdate, cancellationToken: cancellationToken);
            await session.CommitTransactionAsync(cancellationToken);
        }
        catch (NotSupportedException)
        {
            await _messages.InsertOneAsync(doc, cancellationToken: cancellationToken);
            await _outbox.InsertOneAsync(outboxDoc, cancellationToken: cancellationToken);
            var convFilter = Builders<ConversationDocument>.Filter.Eq(c => c.ConversationId, message.ConversationId);
            var convUpdate = Builders<ConversationDocument>.Update.Set(c => c.LastMessageDate, message.SentDate);
            await _conversations.UpdateOneAsync(convFilter, convUpdate, cancellationToken: cancellationToken);
        }

        if (_cache != null)
        {
            await _cache.InvalidateConversationAsync(message.ConversationId);

            if (convDoc != null)
            {
                var otherParticipantIds = convDoc.Participants
                    .Where(p => p.UserId != message.SenderId)
                    .Select(p => p.UserId);
                await _cache.IncrementUnreadBatchAsync(otherParticipantIds, message.ConversationId);
            }
        }

        message.MessageId = messageId;
        return message;
    }

    public async Task<bool> RemoveParticipantAsync(int conversationId, int userId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ConversationDocument>.Filter.Eq(c => c.ConversationId, conversationId);
        var update = Builders<ConversationDocument>.Update.PullFilter(
            c => c.Participants,
            p => p.UserId == userId);

        var result = await _conversations.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);

        if (_cache != null && result.ModifiedCount > 0)
        {
            await _cache.ResetUnreadAsync(userId, conversationId);
            await _cache.InvalidateConversationAsync(conversationId);
        }

        return result.ModifiedCount > 0;
    }

    public async Task MarkMessagesAsReadAsync(int conversationId, int userId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<MessageDocument>.Filter.And(
            Builders<MessageDocument>.Filter.Eq(m => m.ConversationId, conversationId),
            Builders<MessageDocument>.Filter.Ne(m => m.SenderId, userId),
            Builders<MessageDocument>.Filter.Eq(m => m.IsRead, false)
        );
        var update = Builders<MessageDocument>.Update.Set(m => m.IsRead, true);
        await _messages.UpdateManyAsync(filter, update, cancellationToken: cancellationToken);

        if (_cache != null)
        {
            await _cache.ResetUnreadAsync(userId, conversationId);
        }
    }

    public async Task UpdateDeliveryStatusAsync(long messageId, DeliveryStatus status, CancellationToken cancellationToken = default)
    {
        var filter = Builders<MessageDocument>.Filter.Eq(m => m.MessageId, messageId);
        var update = Builders<MessageDocument>.Update.Set(m => m.DeliveryStatus, (int)status);
        await _messages.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<Message>> GetMessagesSinceAsync(int conversationId, long sinceMessageId, int limit = 200, CancellationToken cancellationToken = default)
    {
        var filter = Builders<MessageDocument>.Filter.And(
            Builders<MessageDocument>.Filter.Eq(m => m.ConversationId, conversationId),
            Builders<MessageDocument>.Filter.Gt(m => m.MessageId, sinceMessageId));

        var docs = await _messages
            .Find(filter)
            .Sort(Builders<MessageDocument>.Sort.Ascending(m => m.MessageId))
            .Limit(limit)
            .ToListAsync(cancellationToken);

        var replyIds = docs
            .Where(d => d.ReplyToMessageId.HasValue)
            .Select(d => d.ReplyToMessageId!.Value)
            .Distinct()
            .ToList();

        var replyDocs = new Dictionary<long, MessageDocument>();
        if (replyIds.Count > 0)
        {
            var replyFilter = Builders<MessageDocument>.Filter.In(m => m.MessageId, replyIds);
            var replies = await _messages.Find(replyFilter).ToListAsync(cancellationToken);
            replyDocs = replies.ToDictionary(r => r.MessageId);
        }

        return docs.Select(doc =>
        {
            var msg = MapToMessage(doc);
            if (doc.ReplyToMessageId.HasValue &&
                replyDocs.TryGetValue(doc.ReplyToMessageId.Value, out var replyDoc))
            {
                msg.ReplyToMessage = MapToMessage(replyDoc);
            }
            return msg;
        }).ToList();
    }

    // ========== REACTIONS ==========

    public async Task<MessageReaction?> GetReactionAsync(long messageId, int userId, CancellationToken cancellationToken = default)
    {
        var doc = await _messages.Find(m => m.MessageId == messageId).FirstOrDefaultAsync(cancellationToken);
        var reaction = doc?.Reactions.FirstOrDefault(r => r.UserId == userId);
        return reaction == null ? null : MapToMessageReaction(reaction, messageId);
    }

    public async Task<IEnumerable<MessageReaction>> GetMessageReactionsAsync(long messageId, CancellationToken cancellationToken = default)
    {
        var doc = await _messages.Find(m => m.MessageId == messageId).FirstOrDefaultAsync(cancellationToken);
        if (doc == null) return Enumerable.Empty<MessageReaction>();

        return doc.Reactions
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => MapToMessageReaction(r, messageId))
            .ToList();
    }

    public async Task<MessageReaction> AddReactionAsync(MessageReaction reaction, CancellationToken cancellationToken = default)
    {
        var reactionId = await GetNextSequenceAsync("messageReactions");
        var userInfo = await GetUserInfoAsync(reaction.UserId);

        var embed = new ReactionEmbed
        {
            MessageReactionId = reactionId,
            UserId = reaction.UserId,
            User = userInfo,
            ReactionType = reaction.ReactionType,
            CreatedAt = reaction.CreatedAt
        };

        var filter = Builders<MessageDocument>.Filter.Eq(m => m.MessageId, reaction.MessageId);
        var update = Builders<MessageDocument>.Update.Push(m => m.Reactions, embed);
        await _messages.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);

        reaction.MessageReactionId = reactionId;
        return reaction;
    }

    public async Task UpdateReactionAsync(MessageReaction reaction, CancellationToken cancellationToken = default)
    {
        var filter = Builders<MessageDocument>.Filter.And(
            Builders<MessageDocument>.Filter.Eq(m => m.MessageId, reaction.MessageId),
            Builders<MessageDocument>.Filter.ElemMatch(m => m.Reactions, r => r.UserId == reaction.UserId)
        );
        var update = Builders<MessageDocument>.Update
            .Set("Reactions.$.ReactionType", reaction.ReactionType)
            .Set("Reactions.$.CreatedAt", reaction.CreatedAt);

        await _messages.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }

    public async Task RemoveReactionAsync(MessageReaction reaction, CancellationToken cancellationToken = default)
    {
        var filter = Builders<MessageDocument>.Filter.Eq(m => m.MessageId, reaction.MessageId);
        var update = Builders<MessageDocument>.Update.PullFilter(
            m => m.Reactions, r => r.UserId == reaction.UserId);
        await _messages.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }

    // ========== MAPPING HELPERS ==========

    private static Conversation MapToConversation(ConversationDocument doc)
    {
        return new Conversation
        {
            ConversationId = doc.ConversationId,
            Title = doc.Title,
            IsGroupChat = doc.IsGroupChat,
            CreatedDate = doc.CreatedDate,
            LastMessageDate = doc.LastMessageDate,
            Participants = doc.Participants.Select(p => new ConversationParticipant
            {
                ConversationParticipantId = p.ConversationParticipantId,
                ConversationId = doc.ConversationId,
                UserId = p.UserId,
                JoinedDate = p.JoinedDate,
                LastReadDate = p.LastReadDate,
                User = new User
                {
                    UserId = p.User.UserId,
                    Username = p.User.Username,
                    DisplayName = p.User.DisplayName,
                    ProfilePicture = p.User.ProfilePicture
                }
            }).ToList()
        };
    }

    private static Message MapToMessage(MessageDocument doc)
    {
        return new Message
        {
            MessageId = doc.MessageId,
            ConversationId = doc.ConversationId,
            SenderId = doc.SenderId,
            Content = doc.Content,
            IsRead = doc.IsRead,
            SentDate = doc.SentDate,
            MessageType = doc.MessageType,
            DeliveryStatus = (DeliveryStatus)doc.DeliveryStatus,
            AttachmentUrl = doc.AttachmentUrl,
            AttachmentFileName = doc.AttachmentFileName,
            AttachmentSize = doc.AttachmentSize,
            ReplyToMessageId = doc.ReplyToMessageId,
            Sender = new User
            {
                UserId = doc.Sender.UserId,
                Username = doc.Sender.Username,
                DisplayName = doc.Sender.DisplayName,
                ProfilePicture = doc.Sender.ProfilePicture
            },
            Reactions = doc.Reactions.Select(r => new MessageReaction
            {
                MessageReactionId = r.MessageReactionId,
                MessageId = doc.MessageId,
                UserId = r.UserId,
                ReactionType = r.ReactionType,
                CreatedAt = r.CreatedAt,
                User = new User
                {
                    UserId = r.User.UserId,
                    Username = r.User.Username,
                    DisplayName = r.User.DisplayName,
                    ProfilePicture = r.User.ProfilePicture
                }
            }).ToList()
        };
    }

    private static MessageReaction MapToMessageReaction(ReactionEmbed embed, long messageId)
    {
        return new MessageReaction
        {
            MessageReactionId = embed.MessageReactionId,
            MessageId = messageId,
            UserId = embed.UserId,
            ReactionType = embed.ReactionType,
            CreatedAt = embed.CreatedAt,
            User = new User
            {
                UserId = embed.User.UserId,
                Username = embed.User.Username,
                DisplayName = embed.User.DisplayName,
                ProfilePicture = embed.User.ProfilePicture
            }
        };
    }
}
