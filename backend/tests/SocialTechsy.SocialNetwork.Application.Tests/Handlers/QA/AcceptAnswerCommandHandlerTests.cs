using FluentAssertions;
using Moq;
using SocialTechsy.SocialNetwork.Application.CommandHandlers.Answers;
using SocialTechsy.SocialNetwork.Application.Commands.Answers;
using SocialTechsy.SocialNetwork.Application.Common.Events;
using SocialTechsy.SocialNetwork.Application.Interfaces;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Domain.Entities;
using SocialTechsy.SocialNetwork.Domain.Events;

namespace SocialTechsy.SocialNetwork.Application.Tests.Handlers.QA;

public class AcceptAnswerCommandHandlerTests
{
    private readonly Mock<IAnswerRepository> _answerRepo = new();
    private readonly Mock<IQuestionRepository> _questionRepo = new();
    private readonly Mock<IDomainEventDispatcher> _eventDispatcher = new();
    private readonly Mock<IQuestionEventDispatcher> _questionDispatcher = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private AcceptAnswerCommandHandler CreateSut() =>
        new(_answerRepo.Object, _questionRepo.Object, _eventDispatcher.Object, _questionDispatcher.Object, _unitOfWork.Object);

    public AcceptAnswerCommandHandlerTests()
    {
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _eventDispatcher.Setup(d => d.DispatchAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _questionDispatcher.Setup(d => d.NotifyAnswerAcceptedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Handle_OwnerAccepts_Succeeds_Tc021()
    {
        var question = Question.Create(1, "1234567890", new string('q', 30));
        typeof(Question).GetProperty(nameof(Question.QuestionId))!.SetValue(question, 10);

        var answer = Answer.Create(10, 2, new string('a', 30));
        typeof(Answer).GetProperty(nameof(Answer.AnswerId))!.SetValue(answer, 20);

        _questionRepo.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(question);
        _answerRepo.Setup(r => r.GetByIdAsync(20, It.IsAny<CancellationToken>())).ReturnsAsync(answer);
        _answerRepo.Setup(r => r.AcceptAnswerAsync(20, 10, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var sut = CreateSut();
        var result = await sut.Handle(new AcceptAnswerCommand { UserId = 1, QuestionId = 10, AnswerId = 20 }, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("successfully");
    }

    [Fact]
    public async Task Handle_NonOwner_ReturnsFailure_Tc022()
    {
        var question = Question.Create(1, "1234567890", new string('q', 30));
        typeof(Question).GetProperty(nameof(Question.QuestionId))!.SetValue(question, 10);

        var answer = Answer.Create(10, 2, new string('a', 30));
        typeof(Answer).GetProperty(nameof(Answer.AnswerId))!.SetValue(answer, 20);

        _questionRepo.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(question);
        _answerRepo.Setup(r => r.GetByIdAsync(20, It.IsAny<CancellationToken>())).ReturnsAsync(answer);

        var sut = CreateSut();
        var result = await sut.Handle(new AcceptAnswerCommand { UserId = 99, QuestionId = 10, AnswerId = 20 }, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Not authorized");
        _answerRepo.Verify(r => r.AcceptAnswerAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
