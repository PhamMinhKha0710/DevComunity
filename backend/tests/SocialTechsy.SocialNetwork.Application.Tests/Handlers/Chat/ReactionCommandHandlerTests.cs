using FluentAssertions;
using Moq;
using SocialTechsy.SocialNetwork.Application.CommandHandlers.Chat;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Domain.Entities;
using SocialTechsy.SocialNetwork.Domain.Enums;

namespace SocialTechsy.SocialNetwork.Application.Tests.Handlers.Chat;

public class ReactionCommandHandlerTests
{
    private readonly Mock<IChatRepository> _mockChat = new();
    private readonly Mock<IUserRepository> _mockUser = new();
    private readonly AddReactionCommandHandler _addHandler;
    private readonly RemoveReactionCommandHandler _removeHandler;

    public ReactionCommandHandlerTests()
    {
        _addHandler = new AddReactionCommandHandler(_mockChat.Object, _mockUser.Object);
        _removeHandler = new RemoveReactionCommandHandler(_mockChat.Object);
    }

    [Fact]
    public async Task AddReaction_NewMessage_CreatesReaction_TcC020()
    {
        var message = new Message { MessageId = 1, ConversationId = 1, SenderId = 2 };
        _mockChat.Setup(c => c.GetMessageByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);
        _mockChat.Setup(c => c.GetReactionAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MessageReaction?)null);
        _mockUser.Setup(u => u.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(User.CreateProjected(10, "u", null, null));

        var created = new MessageReaction
        {
            MessageReactionId = 5,
            MessageId = 1,
            UserId = 10,
            ReactionType = "like",
            CreatedAt = DateTime.UtcNow
        };
        _mockChat.Setup(c => c.AddReactionAsync(It.IsAny<MessageReaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        var dto = await _addHandler.Handle(new AddReactionCommand
        {
            MessageId = 1,
            UserId = 10,
            ReactionType = ReactionType.Like
        }, CancellationToken.None);

        dto.Should().NotBeNull();
        dto!.ReactionType.Should().Be("like");
    }

    [Fact]
    public async Task AddReaction_Existing_UpdatesType_TcC021()
    {
        var message = new Message { MessageId = 1, ConversationId = 1, SenderId = 2 };
        _mockChat.Setup(c => c.GetMessageByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);

        var existing = new MessageReaction
        {
            MessageReactionId = 5,
            MessageId = 1,
            UserId = 10,
            ReactionType = "like",
            CreatedAt = DateTime.UtcNow.AddMinutes(-1)
        };
        _mockChat.Setup(c => c.GetReactionAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _mockUser.Setup(u => u.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(User.CreateProjected(10, "u", null, null));

        var dto = await _addHandler.Handle(new AddReactionCommand
        {
            MessageId = 1,
            UserId = 10,
            ReactionType = ReactionType.Love
        }, CancellationToken.None);

        dto.Should().NotBeNull();
        dto!.ReactionType.Should().Be("love");
        _mockChat.Verify(c => c.UpdateReactionAsync(It.IsAny<MessageReaction>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveReaction_Exists_ReturnsTrue()
    {
        var reaction = new MessageReaction { MessageReactionId = 1, MessageId = 1, UserId = 10 };
        _mockChat.Setup(c => c.GetReactionAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reaction);

        var ok = await _removeHandler.Handle(new RemoveReactionCommand
        {
            MessageId = 1,
            UserId = 10
        }, CancellationToken.None);

        ok.Should().BeTrue();
        _mockChat.Verify(c => c.RemoveReactionAsync(reaction, It.IsAny<CancellationToken>()), Times.Once);
    }
}
