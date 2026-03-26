using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SocialTechsy.SocialNetwork.Application;
using SocialTechsy.SocialNetwork.Application.CommandHandlers.Auth;
using SocialTechsy.SocialNetwork.Application.Commands.Auth;
using SocialTechsy.SocialNetwork.Application.Common.DTOs.Auth;
using SocialTechsy.SocialNetwork.Application.Interfaces;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.Tests.Handlers.Auth;

public class RegisterCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IEmailTemplateRenderer> _templateRenderer = new();
    private readonly FrontendConfig _frontendConfig = new() { BaseUrl = "http://localhost:5173" };

    private RegisterCommandHandler CreateSut() =>
        new(
            _userRepo.Object,
            _passwordHasher.Object,
            _unitOfWork.Object,
            _emailService.Object,
            _templateRenderer.Object,
            Options.Create(_frontendConfig),
            Mock.Of<ILogger<RegisterCommandHandler>>());

    public RegisterCommandHandlerTests()
    {
        _passwordHasher.Setup(h => h.HashPassword(It.IsAny<string>())).Returns("hashed");
        _unitOfWork.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task<AuthResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<AuthResponse>>, CancellationToken>((fn, ct) => fn(ct));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsSuccessWithoutTokens_Tc001()
    {
        _userRepo.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _userRepo.Setup(r => r.UsernameExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var createdUser = User.Create("newuser", "new@example.com", "hashed", "New User");
        typeof(User).GetProperty(nameof(User.UserId))!.SetValue(createdUser, 42);
        _userRepo.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User u, CancellationToken _) => u);

        var sut = CreateSut();
        var cmd = new RegisterCommand
        {
            Username = "newuser",
            Email = "new@example.com",
            Password = "password123",
            ConfirmPassword = "password123",
            DisplayName = "New User"
        };

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.AccessToken.Should().BeNull();
        result.RefreshToken.Should().BeNull();
        result.User.Should().NotBeNull();
        result.User!.Username.Should().Be("newuser");
        result.Message.Should().Contain("log in");
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsFailure_Tc002()
    {
        _userRepo.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var sut = CreateSut();
        var result = await sut.Handle(new RegisterCommand
        {
            Username = "u",
            Email = "dup@example.com",
            Password = "password123",
            ConfirmPassword = "password123"
        }, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Email already in use");
        _unitOfWork.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task<AuthResponse>>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Register_WithDuplicateUsername_ReturnsFailure_Tc003()
    {
        _userRepo.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _userRepo.Setup(r => r.UsernameExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var sut = CreateSut();
        var result = await sut.Handle(new RegisterCommand
        {
            Username = "taken",
            Email = "unique@example.com",
            Password = "password123",
            ConfirmPassword = "password123"
        }, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Username already taken");
    }
}
