using FluentAssertions;
using Moq;
using SocialTechsy.SocialNetwork.Application.CommandHandlers.Votes;
using SocialTechsy.SocialNetwork.Application.Commands.Votes;
using SocialTechsy.SocialNetwork.Application.Common.Events;
using SocialTechsy.SocialNetwork.Application.Interfaces;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Domain.Entities;
using SocialTechsy.SocialNetwork.Domain.Enums;
using SocialTechsy.SocialNetwork.Domain.Events;

namespace SocialTechsy.SocialNetwork.Application.Tests.Handlers.QA;

public class VoteQuestionCommandHandlerTests
{
    private readonly Mock<IVoteRepository> _voteRepo = new();
    private readonly Mock<IQuestionRepository> _questionRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IDomainEventDispatcher> _eventDispatcher = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private VoteQuestionCommandHandler CreateSut() =>
        new(_voteRepo.Object, _questionRepo.Object, _userRepo.Object, _eventDispatcher.Object, _unitOfWork.Object,
            likeService: null, activityLog: null, outboxRepository: null);

    public VoteQuestionCommandHandlerTests()
    {
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _eventDispatcher.Setup(d => d.DispatchAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _userRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => User.Create("voter", $"v{id}@t.com", "h", "Voter"));
    }

    [Fact]
    public async Task Handle_OtherUserUpvote_Succeeds_Tc040()
    {
        var question = Question.Create(1, "1234567890", new string('b', 30));
        typeof(Question).GetProperty(nameof(Question.QuestionId))!.SetValue(question, 100);

        _questionRepo.Setup(r => r.GetByIdAsync(100, It.IsAny<CancellationToken>())).ReturnsAsync(question);
        _voteRepo.Setup(r => r.GetUserVoteOnQuestionAsync(2, 100, It.IsAny<CancellationToken>())).ReturnsAsync((Vote?)null);
        _voteRepo.Setup(r => r.AddAsync(It.IsAny<Vote>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vote v, CancellationToken _) => v);
        _voteRepo.Setup(r => r.GetQuestionScoreAsync(100, It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = CreateSut();
        var result = await sut.Handle(new VoteQuestionCommand { UserId = 2, QuestionId = 100, VoteType = VoteType.Up }, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Score.Should().Be(1);
        result.UserVote.Should().Be(VoteType.Up);
    }

    [Fact]
    public async Task Handle_OwnQuestion_ReturnsFailure()
    {
        var question = Question.Create(1, "1234567890", new string('b', 30));
        typeof(Question).GetProperty(nameof(Question.QuestionId))!.SetValue(question, 100);
        _questionRepo.Setup(r => r.GetByIdAsync(100, It.IsAny<CancellationToken>())).ReturnsAsync(question);

        var sut = CreateSut();
        var result = await sut.Handle(new VoteQuestionCommand { UserId = 1, QuestionId = 100, VoteType = VoteType.Up }, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("own");
    }
}
