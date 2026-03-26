using FluentAssertions;
using Moq;
using SocialTechsy.SocialNetwork.Application.CommandHandlers.Auth;
using SocialTechsy.SocialNetwork.Application.Commands.Auth;
using SocialTechsy.SocialNetwork.Application.Interfaces;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.Tests.Handlers.Auth;

public class LoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IAuthTokenIssuer> _tokenIssuer = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private LoginCommandHandler CreateSut() =>
        new(_userRepo.Object, _passwordHasher.Object, _tokenIssuer.Object, _unitOfWork.Object);

    public LoginCommandHandlerTests()
    {
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokens_Tc010()
    {
        var user = User.Create("alice", "alice@example.com", "stored-hash", "Alice");
        _userRepo.Setup(r => r.GetByEmailAsync("alice@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.VerifyPassword("correct", "stored-hash")).Returns(true);
        _tokenIssuer.Setup(t => t.IssueTokensAsync(It.IsAny<User>(), 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(("access", "refresh"));

        var sut = CreateSut();
        var result = await sut.Handle(new LoginCommand { Email = "alice@example.com", Password = "correct", RememberMe = false }, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.AccessToken.Should().Be("access");
        result.RefreshToken.Should().Be("refresh");
        result.User.Should().NotBeNull();
        result.User!.Email.Should().Be("alice@example.com");
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsFailure_Tc011()
    {
        var user = User.Create("alice", "alice@example.com", "stored-hash", "Alice");
        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        var sut = CreateSut();
        var result = await sut.Handle(new LoginCommand { Email = "alice@example.com", Password = "wrong" }, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Invalid email or password");
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_ReturnsSameMessageAsWrongPassword_Tc012()
    {
        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))!
            .ReturnsAsync((User?)null);

        var sut = CreateSut();
        var result = await sut.Handle(new LoginCommand { Email = "missing@example.com", Password = "any" }, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Invalid email or password");
    }
}
