using FluentValidation;
using SocialTechsy.SocialNetwork.Application.Commands.Questions;

namespace SocialTechsy.SocialNetwork.Application.Common.Validators;

public class CreateQuestionCommandValidator : AbstractValidator<CreateQuestionCommand>
{
    public CreateQuestionCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MinimumLength(10).WithMessage("Title must be at least 10 characters.")
            .MaximumLength(500).WithMessage("Title must not exceed 500 characters.");

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Body is required.")
            .MinimumLength(30).WithMessage("Body must be at least 30 characters.");

        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("User ID is required.");
    }
}
