using FluentValidation;
using SocialTechsy.SocialNetwork.Application.Commands.Auth;

namespace SocialTechsy.SocialNetwork.Application.Common.Validators;

public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");
    }
}
