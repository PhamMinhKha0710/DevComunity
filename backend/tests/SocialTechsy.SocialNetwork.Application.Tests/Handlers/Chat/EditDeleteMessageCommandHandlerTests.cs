using FluentAssertions;
using Moq;
using SocialTechsy.SocialNetwork.Application.CommandHandlers.Chat;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.Tests.Handlers.Chat;

public class EditDeleteMessageCommandHandlerTests
{
    private readonly Mock<IChatRepository> _mockRepo = new();
    private readonly EditMessageCommandHandler _editHandler;
    private readonly DeleteMessageCommandHandler _deleteHandler;

    public EditDeleteMessageCommandHandlerTests()
    {
        _editHandler = new EditMessageCommandHandler(_mockRepo.Object);
        _deleteHandler = new DeleteMessageCommandHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task EditMessage_WithinWindow_Sender_ReturnsTrue_TcC017()
    {
        var msg = new Message
        {
            MessageId = 1,
            SenderId = 10,
            Content = "old",
            SentDate = DateTime.UtcNow
        };
        _mockRepo.Setup(r => r.GetMessageByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(msg);

        var ok = await _editHandler.Handle(new EditMessageCommand
        {
            MessageId = 1,
            UserId = 10,
            NewContent = "new content"
        }, CancellationToken.None);

        ok.Should().BeTrue();
        msg.Content.Should().Be("new content");
    }

    [Fact]
    public async Task EditMessage_ExpiredWindow_ReturnsFalse()
    {
        var msg = new Message
        {
            MessageId = 1,
            SenderId = 10,
            Content = "old",
            SentDate = DateTime.UtcNow.AddMinutes(-20)
        };
        _mockRepo.Setup(r => r.GetMessageByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(msg);

        var ok = await _editHandler.Handle(new EditMessageCommand
        {
            MessageId = 1,
            UserId = 10,
            NewContent = "new"
        }, CancellationToken.None);

        ok.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteMessage_Sender_ReturnsTrue_TcC018()
    {
        var msg = new Message
        {
            MessageId = 2,
            SenderId = 10,
            Content = "x",
            SentDate = DateTime.UtcNow
        };
        _mockRepo.Setup(r => r.GetMessageByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(msg);

        var ok = await _deleteHandler.Handle(new DeleteMessageCommand
        {
            MessageId = 2,
            UserId = 10
        }, CancellationToken.None);

        ok.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteMessage_NotSender_ReturnsFalse()
    {
        var msg = new Message
        {
            MessageId = 2,
            SenderId = 10,
            Content = "x",
            SentDate = DateTime.UtcNow
        };
        _mockRepo.Setup(r => r.GetMessageByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(msg);

        var ok = await _deleteHandler.Handle(new DeleteMessageCommand
        {
            MessageId = 2,
            UserId = 99
        }, CancellationToken.None);

        ok.Should().BeFalse();
    }
}
