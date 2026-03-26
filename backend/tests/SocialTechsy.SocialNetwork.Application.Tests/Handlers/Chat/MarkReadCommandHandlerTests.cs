using Moq;
using SocialTechsy.SocialNetwork.Application.CommandHandlers.Chat;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;

namespace SocialTechsy.SocialNetwork.Application.Tests.Handlers.Chat;

public class MarkReadCommandHandlerTests
{
    private readonly Mock<IChatRepository> _mockChat = new();
    private readonly Mock<IChatMessageBroker> _mockBroker = new();
    private readonly MarkConversationReadCommandHandler _handler;

    public MarkReadCommandHandlerTests()
    {
        _handler = new MarkConversationReadCommandHandler(_mockChat.Object, _mockBroker.Object);
    }

    [Fact]
    public async Task Handle_NoWatermark_CallsMarkMessagesAsRead_TcC012()
    {
        await _handler.Handle(new MarkConversationReadCommand
        {
            ConversationId = 1,
            UserId = 10,
            LastReadMessageId = 0
        }, CancellationToken.None);

        _mockChat.Verify(c => c.MarkMessagesAsReadAsync(1, 10, It.IsAny<CancellationToken>()), Times.Once);
        _mockChat.Verify(c => c.UpdateReadWatermarkAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithWatermark_CallsUpdateWatermark()
    {
        await _handler.Handle(new MarkConversationReadCommand
        {
            ConversationId = 1,
            UserId = 10,
            LastReadMessageId = 42
        }, CancellationToken.None);

        _mockChat.Verify(c => c.UpdateReadWatermarkAsync(1, 10, 42, It.IsAny<CancellationToken>()), Times.Once);
    }
}
