using FluentAssertions;
using Moq;
using SocialTechsy.SocialNetwork.Application.CommandHandlers.Chat;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.Tests.Handlers.Chat;

public class SendMessageCommandHandlerTests
{
    private readonly Mock<IChatRepository> _mockChat = new();
    private readonly Mock<IUserRepository> _mockUser = new();
    private readonly SendMessageCommandHandler _handler;

    public SendMessageCommandHandlerTests()
    {
        _handler = new SendMessageCommandHandler(_mockChat.Object, _mockUser.Object);
    }

    [Fact]
    public async Task Handle_ValidConversation_ReturnsResult_TcC010()
    {
        var conv = new Conversation
        {
            ConversationId = 1,
            Participants = new List<ConversationParticipant>
            {
                new() { UserId = 1 },
                new() { UserId = 2 }
            }
        };
        _mockChat.Setup(c => c.GetConversationByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conv);

        var sender = User.CreateProjected(1, "alice", null, null);
        _mockUser.Setup(u => u.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(sender);

        var savedMessage = new Message
        {
            MessageId = 100,
            ConversationId = 1,
            SenderId = 1,
            Content = "hello",
            SentDate = DateTime.UtcNow
        };
        _mockChat.Setup(c => c.AddMessageAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedMessage);

        var result = await _handler.Handle(new SendMessageCommand
        {
            ConversationId = 1,
            SenderId = 1,
            Content = "hello"
        }, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Message.MessageId.Should().Be(100);
        result.Message.Content.Should().Be("hello");
    }

    [Fact]
    public async Task Handle_ConversationNotFound_ReturnsNull_TcC013()
    {
        _mockChat.Setup(c => c.GetConversationByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Conversation?)null);

        var result = await _handler.Handle(new SendMessageCommand
        {
            ConversationId = 99,
            SenderId = 1,
            Content = "x"
        }, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_UserNotParticipant_ReturnsNull()
    {
        var conv = new Conversation
        {
            ConversationId = 1,
            Participants = new List<ConversationParticipant>
            {
                new() { UserId = 2 }
            }
        };
        _mockChat.Setup(c => c.GetConversationByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conv);

        var result = await _handler.Handle(new SendMessageCommand
        {
            ConversationId = 1,
            SenderId = 1,
            Content = "hello"
        }, CancellationToken.None);

        result.Should().BeNull();
    }
}
