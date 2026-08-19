using FluentValidation;
using FoodDiary.Application.Abstractions.Nutrition.Common;
using FoodDiary.Application.Meals.Common.Validation;
using FoodDiary.Application.Meals.Common.Validators;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Meals.Commands.UpdateMeal;

public sealed class UpdateMealCommandValidator : AbstractValidator<UpdateMealCommand> {
    public UpdateMealCommandValidator() {
        ConfigureUserRules();
        ConfigureMealRules();
        ConfigureItemRules();
        ConfigureManualNutritionRules();
    }

    private void ConfigureUserRules() {
        RuleFor(c => c.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Validation.Required")
            .WithMessage("UserId is required.")
            .Must(id => id is not null && id.Value != Guid.Empty)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("UserId is invalid.");

        RuleFor(c => c.MealId)
            .NotEqual(Guid.Empty)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Meal id must not be empty.");
    }

    private void ConfigureMealRules() {
        RuleFor(c => c.MealType)
            .Must(EnumValueParser.CanParseOptional<MealType>)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Unknown meal type value.");

        RuleFor(c => c)
            .Must(c => c.Items is { Count: > 0 } ||
                       (c.AiSessions is { Count: > 0 } && c.AiSessions.Any(s => s is { Items.Count: > 0 })))
            .WithErrorCode("Validation.Required")
            .WithMessage("At least one item or AI session with items is required.");

        RuleFor(c => c.PreMealSatietyLevel)
            .Must(level => level == 0 || level is >= 1 and <= 5)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Satiety level must be between 1 and 5.");

        RuleFor(c => c.PostMealSatietyLevel)
            .Must(level => level == 0 || level is >= 1 and <= 5)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Satiety level must be between 1 and 5.");
    }

    private void ConfigureItemRules() {
        RuleForEach(c => c.Items)
            .NotNull()
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Meal items must not contain null elements.");

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
                .Must(amount => !double.IsNaN(amount) && !double.IsInfinity(amount) && amount is > 0 and <= 1_000_000d)
                .WithErrorCode("Validation.Invalid")
                .WithMessage("Amount must be in range (0, 1000000].");
        });

        RuleForEach(c => c.AiSessions)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Validation.Invalid")
            .WithMessage("AI sessions must not contain null elements.")
            .SetValidator(new MealAiSessionInputValidator());
    }

    private void ConfigureManualNutritionRules() {
        When(c => !c.IsNutritionAutoCalculated, () => {
            RuleFor(c => c.ManualCalories)
                .NotNull().WithErrorCode("Validation.Required").WithMessage("ManualCalories is required.")
                .GreaterThanOrEqualTo(0).WithErrorCode("Validation.Invalid").WithMessage("Values must be greater than or equal to 0.")
                .LessThanOrEqualTo(ManualNutritionLimits.MaxCalories)
                .WithErrorCode("Validation.Invalid")
                .WithMessage(ManualNutritionLimits.MaxCaloriesErrorMessage);
            RuleFor(c => c.ManualProteins)
                .NotNull().WithErrorCode("Validation.Required").WithMessage("ManualProteins is required.")
                .GreaterThanOrEqualTo(0).WithErrorCode("Validation.Invalid").WithMessage("Values must be greater than or equal to 0.")
                .LessThanOrEqualTo(ManualNutritionLimits.MaxNutrient)
                .WithErrorCode("Validation.Invalid")
                .WithMessage(ManualNutritionLimits.MaxNutrientErrorMessage);
            RuleFor(c => c.ManualFats)
                .NotNull().WithErrorCode("Validation.Required").WithMessage("ManualFats is required.")
                .GreaterThanOrEqualTo(0).WithErrorCode("Validation.Invalid").WithMessage("Values must be greater than or equal to 0.")
                .LessThanOrEqualTo(ManualNutritionLimits.MaxNutrient)
                .WithErrorCode("Validation.Invalid")
                .WithMessage(ManualNutritionLimits.MaxNutrientErrorMessage);
            RuleFor(c => c.ManualCarbs)
                .NotNull().WithErrorCode("Validation.Required").WithMessage("ManualCarbs is required.")
                .GreaterThanOrEqualTo(0).WithErrorCode("Validation.Invalid").WithMessage("Values must be greater than or equal to 0.")
                .LessThanOrEqualTo(ManualNutritionLimits.MaxNutrient)
                .WithErrorCode("Validation.Invalid")
                .WithMessage(ManualNutritionLimits.MaxNutrientErrorMessage);
            RuleFor(c => c.ManualFiber)
                .NotNull().WithErrorCode("Validation.Required").WithMessage("ManualFiber is required.")
                .GreaterThanOrEqualTo(0).WithErrorCode("Validation.Invalid").WithMessage("Values must be greater than or equal to 0.")
                .LessThanOrEqualTo(ManualNutritionLimits.MaxNutrient)
                .WithErrorCode("Validation.Invalid")
                .WithMessage(ManualNutritionLimits.MaxNutrientErrorMessage);
            RuleFor(c => c.ManualAlcohol)
                .GreaterThanOrEqualTo(0)
                .When(c => c.ManualAlcohol.HasValue)
                .WithErrorCode("Validation.Invalid")
                .WithMessage("Values must be greater than or equal to 0.")
                .LessThanOrEqualTo(ManualNutritionLimits.MaxNutrient)
                .When(c => c.ManualAlcohol.HasValue)
                .WithErrorCode("Validation.Invalid")
                .WithMessage(ManualNutritionLimits.MaxNutrientErrorMessage);
        });
    }
}
