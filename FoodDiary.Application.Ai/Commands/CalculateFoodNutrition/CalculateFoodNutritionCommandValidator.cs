using FluentValidation;

namespace FoodDiary.Application.Ai.Commands.CalculateFoodNutrition;

public sealed class CalculateFoodNutritionCommandValidator : AbstractValidator<CalculateFoodNutritionCommand> {
    public CalculateFoodNutritionCommandValidator() {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithErrorCode("Validation.Required");

        RuleFor(x => x.Items)
            .NotEmpty()
            .WithErrorCode("Validation.Required");

        RuleForEach(x => x.Items).ChildRules(item => {
            item.RuleFor(x => x.NameEn)
                .NotEmpty()
                .MaximumLength(256);

            item.RuleFor(x => x.Unit)
                .NotEmpty()
                .MaximumLength(32);

            item.RuleFor(x => x.Amount)
                .GreaterThan(0);

            item.RuleFor(x => x.Confidence)
                .GreaterThanOrEqualTo(0);
        });
    }
}
