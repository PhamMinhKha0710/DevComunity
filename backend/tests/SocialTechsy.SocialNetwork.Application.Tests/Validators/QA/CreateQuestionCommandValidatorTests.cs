using FluentAssertions;
using SocialTechsy.SocialNetwork.Application.Commands.Questions;
using SocialTechsy.SocialNetwork.Application.Common.Validators;

namespace SocialTechsy.SocialNetwork.Application.Tests.Validators.QA;

public class CreateQuestionCommandValidatorTests
{
    private readonly CreateQuestionCommandValidator _sut = new();

    [Fact]
    public void Validate_ShortTitle_Fails()
    {
        var cmd = new CreateQuestionCommand
        {
            UserId = 1,
            Title = "short",
            Body = new string('a', 30)
        };
        var result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateQuestionCommand.Title));
    }

    [Fact]
    public void Validate_ShortBody_Fails()
    {
        var cmd = new CreateQuestionCommand
        {
            UserId = 1,
            Title = "1234567890",
            Body = "short"
        };
        var result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateQuestionCommand.Body));
    }
}
