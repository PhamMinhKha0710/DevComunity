using FluentAssertions;
using SocialTechsy.SocialNetwork.Application.Commands.Answers;
using SocialTechsy.SocialNetwork.Application.Common.Validators;

namespace SocialTechsy.SocialNetwork.Application.Tests.Validators.QA;

public class CreateAnswerCommandValidatorTests
{
    private readonly CreateAnswerCommandValidator _sut = new();

    [Fact]
    public void Validate_ShortBody_Fails()
    {
        var cmd = new CreateAnswerCommand
        {
            QuestionId = 1,
            UserId = 1,
            Body = "short"
        };
        var result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAnswerCommand.Body));
    }
}
