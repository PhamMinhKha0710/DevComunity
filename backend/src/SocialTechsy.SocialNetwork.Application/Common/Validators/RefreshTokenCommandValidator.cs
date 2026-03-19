using FluentValidation;
using SocialTechsy.SocialNetwork.Application.Commands.Auth;

namespace SocialTechsy.SocialNetwork.Application.Common.Validators;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.");
    }
}
