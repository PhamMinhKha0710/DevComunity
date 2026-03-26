using FluentValidation;
using SocialTechsy.SocialNetwork.Application.Commands.Auth;

namespace SocialTechsy.SocialNetwork.Application.Common.Validators;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Must(p => p.Any(char.IsUpper)).WithMessage("Password must contain at least one uppercase letter.")
            .Must(p => p.Any(char.IsDigit)).WithMessage("Password must contain at least one number.");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword).WithMessage("Confirm password must match new password.");

        RuleFor(x => x.VerificationCode)
            .NotEmpty().WithMessage("Verification code is required.")
            .Length(6).WithMessage("Verification code must be 6 digits.");
    }
}
