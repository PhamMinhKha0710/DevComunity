using FluentAssertions;
using Moq;
using SocialTechsy.SocialNetwork.Application.CommandHandlers.Questions;
using SocialTechsy.SocialNetwork.Application.Commands.Questions;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Question;
using SocialTechsy.SocialNetwork.Application.Interfaces;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.Tests.Handlers.QA;

public class CreateQuestionCommandHandlerTests
{
    private readonly Mock<IQuestionRepository> _questionRepo = new();
    private readonly Mock<ITagRepository> _tagRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private CreateQuestionCommandHandler CreateSut() =>
        new(_questionRepo.Object, _tagRepo.Object, _userRepo.Object, _unitOfWork.Object, null);

    public CreateQuestionCommandHandlerTests()
    {
        _unitOfWork.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task<QuestionDto>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<QuestionDto>>, CancellationToken>((fn, ct) => fn(ct));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _userRepo.Setup(r => r.UpdateReputationAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _questionRepo.Setup(r => r.AddAsync(It.IsAny<Question>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Question q, CancellationToken _) =>
            {
                typeof(Question).GetProperty(nameof(Question.QuestionId))!.SetValue(q, 42);
                return q;
            });
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsQuestionDtoWithId_Tc001()
    {
        var cmd = new CreateQuestionCommand
        {
            UserId = 1,
            Title = "1234567890",
            Body = new string('a', 30),
            Tags = new List<string>()
        };

        var sut = CreateSut();
        var result = await sut.Handle(cmd, CancellationToken.None);

        result.QuestionId.Should().Be(42);
        result.Title.Should().Be(cmd.Title);
        result.Body.Should().Be(cmd.Body);
        _questionRepo.Verify(r => r.AddAsync(It.IsAny<Question>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
