using FluentAssertions;
using SocialTechsy.SocialNetwork.Application.Commands.Auth;
using SocialTechsy.SocialNetwork.Application.Common.Validators;

namespace SocialTechsy.SocialNetwork.Application.Tests.Validators.Auth;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _sut = new();

    [Fact]
    public void Validate_InvalidEmail_Fails()
    {
        var cmd = new LoginCommand { Email = "not-an-email", Password = "secret12" };
        var result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_ShortPassword_Fails()
    {
        var cmd = new LoginCommand { Email = "a@b.com", Password = "12345" };
        var result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Valid_Succeeds()
    {
        var cmd = new LoginCommand { Email = "user@example.com", Password = "secret12" };
        var result = _sut.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }
}
