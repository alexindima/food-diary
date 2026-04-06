using FluentValidation;

namespace FoodDiary.Application.Ai.Commands.ParseFoodText;

public sealed class ParseFoodTextCommandValidator : AbstractValidator<ParseFoodTextCommand> {
    public ParseFoodTextCommandValidator() {
        RuleFor(x => x.Text)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("Food description text is required.")
            .MaximumLength(2048)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Food description text must not exceed 2048 characters.");
    }
}
