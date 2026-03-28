using FluentValidation;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Consumptions.Commands.CreateConsumption;

public class CreateConsumptionCommandValidator : AbstractValidator<CreateConsumptionCommand> {
    public CreateConsumptionCommandValidator() {
        RuleFor(c => c.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Validation.Required")
            .WithMessage("UserId is required.")
            .Must(id => id is not null && id.Value != Guid.Empty)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("UserId is invalid.");

        RuleFor(c => c.MealType)
            .Must(mealType => string.IsNullOrWhiteSpace(mealType) || Enum.TryParse<MealType>(mealType, true, out _))
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Unknown meal type value.");

        RuleFor(c => c)
            .Must(c => c.Items is { Count: > 0 } ||
                       (c.AiSessions is { Count: > 0 } && c.AiSessions.Any(s => s.Items.Count > 0)))
            .WithErrorCode("Validation.Required")
            .WithMessage("At least one item or AI session with items is required.");

        RuleFor(c => c.PreMealSatietyLevel)
            .InclusiveBetween(0, 9)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Satiety level must be between 0 and 9.");

        RuleFor(c => c.PostMealSatietyLevel)
            .InclusiveBetween(0, 9)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Satiety level must be between 0 and 9.");

        RuleForEach(c => c.Items).ChildRules(item => {
            item.RuleFor(i => i)
                .Must(i => i.ProductId.HasValue || i.RecipeId.HasValue)
                .WithErrorCode("Validation.Invalid")
                .WithMessage("Each item must contain productId or recipeId.");

            item.RuleFor(i => i)
                .Must(i => !(i.ProductId.HasValue && i.RecipeId.HasValue))
                .WithErrorCode("Validation.Invalid")
                .WithMessage("Item cannot contain both productId and recipeId.");

            item.RuleFor(i => i.Amount)
                .GreaterThan(0)
                .WithErrorCode("Validation.Invalid")
                .WithMessage("Amount must be greater than zero.");
        });

        When(c => !c.IsNutritionAutoCalculated, () => {
            RuleFor(c => c.ManualCalories)
                .NotNull().WithErrorCode("Validation.Required").WithMessage("ManualCalories is required.")
                .GreaterThanOrEqualTo(0).WithErrorCode("Validation.Invalid").WithMessage("Values must be greater than or equal to 0.");
            RuleFor(c => c.ManualProteins)
                .NotNull().WithErrorCode("Validation.Required").WithMessage("ManualProteins is required.")
                .GreaterThanOrEqualTo(0).WithErrorCode("Validation.Invalid").WithMessage("Values must be greater than or equal to 0.");
            RuleFor(c => c.ManualFats)
                .NotNull().WithErrorCode("Validation.Required").WithMessage("ManualFats is required.")
                .GreaterThanOrEqualTo(0).WithErrorCode("Validation.Invalid").WithMessage("Values must be greater than or equal to 0.");
            RuleFor(c => c.ManualCarbs)
                .NotNull().WithErrorCode("Validation.Required").WithMessage("ManualCarbs is required.")
                .GreaterThanOrEqualTo(0).WithErrorCode("Validation.Invalid").WithMessage("Values must be greater than or equal to 0.");
            RuleFor(c => c.ManualFiber)
                .NotNull().WithErrorCode("Validation.Required").WithMessage("ManualFiber is required.")
                .GreaterThanOrEqualTo(0).WithErrorCode("Validation.Invalid").WithMessage("Values must be greater than or equal to 0.");
            RuleFor(c => c.ManualAlcohol)
                .GreaterThanOrEqualTo(0)
                .When(c => c.ManualAlcohol.HasValue)
                .WithErrorCode("Validation.Invalid")
                .WithMessage("Values must be greater than or equal to 0.");
        });
    }
}
