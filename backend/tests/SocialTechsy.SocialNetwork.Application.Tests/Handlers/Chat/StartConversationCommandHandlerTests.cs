using FluentAssertions;
using Moq;
using SocialTechsy.SocialNetwork.Application.CommandHandlers.Chat;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.Tests.Handlers.Chat;

public class StartConversationCommandHandlerTests
{
    private readonly Mock<IChatRepository> _mockRepo = new();
    private readonly StartConversationCommandHandler _handler;

    public StartConversationCommandHandlerTests()
    {
        _handler = new StartConversationCommandHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_NoExistingConversation_CreatesNew_TcC001()
    {
        _mockRepo.Setup(r => r.GetConversationBetweenUsersAsync(1, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Conversation?)null);

        _mockRepo.Setup(r => r.CreateConversationAsync(It.IsAny<Conversation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Conversation c, CancellationToken _) =>
            {
                c.ConversationId = 99;
                return c;
            });

        var result = await _handler.Handle(new StartConversationCommand
        {
            InitiatorId = 1,
            RecipientId = 2
        }, CancellationToken.None);

        result.ConversationId.Should().Be(99);
        _mockRepo.Verify(r => r.CreateConversationAsync(It.IsAny<Conversation>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingConversation_ReturnsExistingWithoutCreate_TcC001()
    {
        var existing = new Conversation
        {
            ConversationId = 5,
            CreatedDate = DateTime.UtcNow.AddDays(-1),
            LastMessageDate = DateTime.UtcNow,
            IsGroupChat = false
        };

        _mockRepo.Setup(r => r.GetConversationBetweenUsersAsync(1, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await _handler.Handle(new StartConversationCommand
        {
            InitiatorId = 1,
            RecipientId = 2
        }, CancellationToken.None);

        result.ConversationId.Should().Be(5);
        _mockRepo.Verify(r => r.CreateConversationAsync(It.IsAny<Conversation>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
