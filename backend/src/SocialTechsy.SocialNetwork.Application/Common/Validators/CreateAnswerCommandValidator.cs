using FluentValidation;
using SocialTechsy.SocialNetwork.Application.Commands.Answers;

namespace SocialTechsy.SocialNetwork.Application.Common.Validators;

public class CreateAnswerCommandValidator : AbstractValidator<CreateAnswerCommand>
{
    public CreateAnswerCommandValidator()
    {
        RuleFor(x => x.QuestionId)
            .GreaterThan(0).WithMessage("Question ID is required.");

        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("User ID is required.");

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Body is required.")
            .MinimumLength(30).WithMessage("Body must be at least 30 characters.");
    }
}
