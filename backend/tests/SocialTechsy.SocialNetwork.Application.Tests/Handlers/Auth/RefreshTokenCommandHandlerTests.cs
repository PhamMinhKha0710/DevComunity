using FluentAssertions;
using Moq;
using SocialTechsy.SocialNetwork.Application.CommandHandlers.Auth;
using SocialTechsy.SocialNetwork.Application.Commands.Auth;
using SocialTechsy.SocialNetwork.Application.Interfaces;
using SocialTechsy.SocialNetwork.Application.Interfaces.Repositories;
using SocialTechsy.SocialNetwork.Application.Interfaces.Services;
using SocialTechsy.SocialNetwork.Domain.Entities;

namespace SocialTechsy.SocialNetwork.Application.Tests.Handlers.Auth;

public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<IRefreshTokenRepository> _refreshRepo = new();
    private readonly Mock<IAuthTokenIssuer> _tokenIssuer = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private RefreshTokenCommandHandler CreateSut() =>
        new(_refreshRepo.Object, _tokenIssuer.Object, _unitOfWork.Object);

    public RefreshTokenCommandHandlerTests()
    {
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    [Fact]
    public async Task Refresh_WithValidToken_ReturnsNewPair_Tc020()
    {
        var user = User.Create("u", "u@e.com", "h", "U");
        var stored = new RefreshToken
        {
            Token = "old-rt",
            UserId = 1,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            User = user
        };
        _refreshRepo.Setup(r => r.GetByTokenAsync("old-rt", It.IsAny<CancellationToken>())).ReturnsAsync(stored);
        _tokenIssuer.Setup(t => t.IssueTokensAsync(It.IsAny<User>(), 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(("new-access", "new-refresh"));

        var sut = CreateSut();
        var result = await sut.Handle(new RefreshTokenCommand { RefreshToken = "old-rt" }, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.AccessToken.Should().Be("new-access");
        result.RefreshToken.Should().Be("new-refresh");
        _refreshRepo.Verify(r => r.UpdateAsync(It.Is<RefreshToken>(t => t.RevokedAt != null && t.ReplacedByToken == "new-refresh"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Refresh_WithExpiredToken_ReturnsFailure_Tc021()
    {
        var user = User.Create("u", "u@e.com", "h", "U");
        var stored = new RefreshToken
        {
            Token = "expired",
            UserId = 1,
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-8),
            User = user
        };
        _refreshRepo.Setup(r => r.GetByTokenAsync("expired", It.IsAny<CancellationToken>())).ReturnsAsync(stored);

        var sut = CreateSut();
        var result = await sut.Handle(new RefreshTokenCommand { RefreshToken = "expired" }, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message!.ToLowerInvariant().Should().Contain("expired");
    }

    [Fact]
    public async Task Refresh_WithRevokedToken_RevokesAllSessions_Tc022()
    {
        var user = User.Create("u", "u@e.com", "h", "U");
        var stored = new RefreshToken
        {
            Token = "revoked",
            UserId = 5,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            RevokedAt = DateTime.UtcNow.AddMinutes(-1),
            User = user
        };
        _refreshRepo.Setup(r => r.GetByTokenAsync("revoked", It.IsAny<CancellationToken>())).ReturnsAsync(stored);

        var sut = CreateSut();
        var result = await sut.Handle(new RefreshTokenCommand { RefreshToken = "revoked" }, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message!.ToLowerInvariant().Should().Contain("revoked");
        _refreshRepo.Verify(r => r.RevokeAllByUserIdAsync(5, It.IsAny<CancellationToken>()), Times.Once);
    }
}
