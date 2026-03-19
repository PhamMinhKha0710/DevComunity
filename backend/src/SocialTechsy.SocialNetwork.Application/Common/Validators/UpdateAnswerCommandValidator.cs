using FluentValidation;
using SocialTechsy.SocialNetwork.Application.Commands.Answers;

namespace SocialTechsy.SocialNetwork.Application.Common.Validators;

public class UpdateAnswerCommandValidator : AbstractValidator<UpdateAnswerCommand>
{
    public UpdateAnswerCommandValidator()
    {
        RuleFor(x => x.AnswerId)
            .GreaterThan(0).WithMessage("Answer ID is required.");

        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("User ID is required.");

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Body is required.")
            .MinimumLength(30).WithMessage("Body must be at least 30 characters.");
    }
}
