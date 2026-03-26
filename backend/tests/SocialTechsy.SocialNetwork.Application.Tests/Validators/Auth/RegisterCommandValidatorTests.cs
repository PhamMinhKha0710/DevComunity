using FluentAssertions;
using SocialTechsy.SocialNetwork.Application.Commands.Auth;
using SocialTechsy.SocialNetwork.Application.Common.Validators;

namespace SocialTechsy.SocialNetwork.Application.Tests.Validators.Auth;

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _sut = new();

    [Fact]
    public void Validate_ShortPassword_ReturnsError_Tc004()
    {
        var cmd = ValidCommand();
        cmd.Password = "123";
        cmd.ConfirmPassword = "123";
        var result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterCommand.Password));
    }

    [Fact]
    public void Validate_InvalidEmail_ReturnsError_Tc005()
    {
        var cmd = ValidCommand();
        cmd.Email = "abc@";
        var result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterCommand.Email));
    }

    [Fact]
    public void Validate_ValidCommand_Succeeds()
    {
        var result = _sut.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    private static RegisterCommand ValidCommand() => new()
    {
        Username = "validuser",
        Email = "valid@example.com",
        Password = "secret12",
        ConfirmPassword = "secret12",
        DisplayName = "Valid"
    };
}
