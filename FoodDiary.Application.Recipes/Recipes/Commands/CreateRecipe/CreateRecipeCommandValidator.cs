using FoodDiary.Application.Recipes.Recipes.Common;
using FoodDiary.Application.Recipes.Recipes.Common.Validators;
using FluentValidation;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Abstractions.Nutrition.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.Entities.Recipes;

namespace FoodDiary.Application.Recipes.Recipes.Commands.CreateRecipe;

public sealed class CreateRecipeCommandValidator : AbstractValidator<CreateRecipeCommand> {
    public CreateRecipeCommandValidator() {
        ConfigureIdentityRules();
        ConfigureBaseRecipeRules();
        ConfigureStepRules();
        ConfigureNutritionRules();
    }

    private void ConfigureIdentityRules() {
        RuleFor(x => x.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .Must(id => id is not null && id.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("Name is required")
            .MaximumLength(Recipe.NameMaxLength)
            .WithErrorCode("Validation.Invalid");
    }

    private void ConfigureBaseRecipeRules() {
        RuleFor(x => x.Description)
            .MaximumLength(Recipe.DescriptionMaxLength);

        RuleFor(x => x.Comment)
            .MaximumLength(Recipe.CommentMaxLength);

        RuleFor(x => x.Category)
            .MaximumLength(Recipe.CategoryMaxLength);

        RuleFor(x => x.ImageUrl)
            .MaximumLength(Recipe.ImageUrlMaxLength);

        RuleFor(x => x.Servings)
            .GreaterThan(0)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Servings must be greater than zero");

        RuleFor(x => x.PrepTime)
            .GreaterThanOrEqualTo(0)
            .When(x => x.PrepTime.HasValue)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("PrepTime must be greater than or equal to zero");

        RuleFor(x => x.CookTime)
            .GreaterThan(0)
            .When(x => x.CookTime.HasValue)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("CookTime must be greater than zero");

        RuleFor(x => x.Visibility)
            .NotEmpty()
            .Must(BeValidVisibility)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Invalid visibility level");
    }

    private void ConfigureStepRules() {
        RuleFor(x => x.Steps)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Validation.Required")
            .WithMessage("Steps are required")
            .Must(steps => steps.Count > 0)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Recipe must contain at least one step");

        RuleFor(x => x.Steps)
            .Must(static steps => HaveUniqueEffectiveStepOrder(steps!))
            .When(x => x.Steps is { Count: > 0 })
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Step order values must be unique");

        RuleForEach(x => x.Steps)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Recipe steps must not contain null elements")
            .SetValidator(new RecipeStepInputValidator());
    }

    private void ConfigureNutritionRules() {
        RuleFor(x => x)
            .Must(cmd => cmd.CalculateNutritionAutomatically || HasManualNutrition(cmd))
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Manual nutrition values are required when automatic calculation is disabled.");

        RuleFor(x => x.ManualCalories)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(ManualNutritionLimits.MaxCalories)
            .When(x => !x.CalculateNutritionAutomatically);

        RuleFor(x => x.ManualProteins)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(ManualNutritionLimits.MaxNutrient)
            .When(x => !x.CalculateNutritionAutomatically);

        RuleFor(x => x.ManualFats)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(ManualNutritionLimits.MaxNutrient)
            .When(x => !x.CalculateNutritionAutomatically);

        RuleFor(x => x.ManualCarbs)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(ManualNutritionLimits.MaxNutrient)
            .When(x => !x.CalculateNutritionAutomatically);

        RuleFor(x => x.ManualFiber)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(ManualNutritionLimits.MaxNutrient)
            .When(x => !x.CalculateNutritionAutomatically);

        RuleFor(x => x.ManualAlcohol)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(ManualNutritionLimits.MaxNutrient)
            .When(x => !x.CalculateNutritionAutomatically && x.ManualAlcohol.HasValue);
    }

    private static bool BeValidVisibility(string visibility) =>
        SharedEnumValueParser.CanParse<Visibility>(visibility);

    private static bool HasManualNutrition(CreateRecipeCommand command) =>
        command is { ManualCalories: not null, ManualProteins: not null, ManualFats: not null, ManualCarbs: not null, ManualFiber: not null };

    private static bool HaveUniqueEffectiveStepOrder(IReadOnlyList<RecipeStepInput> steps) {
        if (steps.Any(static step => step is null)) {
            return false;
        }

        var orders = new HashSet<int>();
        return steps.Select((step, index) => step.Order > 0 ? step.Order : index + 1).All(orders.Add);
    }
}
