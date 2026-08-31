using FluentValidation;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Recipes.Common;
using FoodDiary.Application.Abstractions.Nutrition.Common;
using FoodDiary.Application.Recipes.Common.Validators;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.Entities.Recipes;

namespace FoodDiary.Application.Recipes.Commands.UpdateRecipe;

public sealed class UpdateRecipeCommandValidator : AbstractValidator<UpdateRecipeCommand> {
    public UpdateRecipeCommandValidator() {
        ConfigureIdentityRules();
        ConfigureBaseRecipeRules();
        ConfigureClearRules();
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

        RuleFor(x => x.RecipeId)
            .NotEqual(Guid.Empty)
            .WithErrorCode("Validation.Required")
            .WithMessage("RecipeId is required");

        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("Name must not be empty")
            .MaximumLength(Recipe.NameMaxLength)
            .WithErrorCode("Validation.Invalid")
            .When(x => x.Name is not null);
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
            .When(x => x.Servings.HasValue)
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
            .Must(BeValidVisibility)
            .When(x => !string.IsNullOrWhiteSpace(x.Visibility))
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Invalid visibility level");
    }

    private void ConfigureClearRules() {
        RuleFor(x => x)
            .Must(x => !(x.ClearDescription && !string.IsNullOrWhiteSpace(x.Description)))
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Description cannot be provided when ClearDescription is true");

        RuleFor(x => x)
            .Must(x => !(x.ClearComment && !string.IsNullOrWhiteSpace(x.Comment)))
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Comment cannot be provided when ClearComment is true");

        RuleFor(x => x)
            .Must(x => !(x.ClearCategory && !string.IsNullOrWhiteSpace(x.Category)))
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Category cannot be provided when ClearCategory is true");

        RuleFor(x => x)
            .Must(x => !(x.ClearImageUrl && !string.IsNullOrWhiteSpace(x.ImageUrl)))
            .WithErrorCode("Validation.Invalid")
            .WithMessage("ImageUrl cannot be provided when ClearImageUrl is true");

        RuleFor(x => x)
            .Must(x => !(x.ClearImageAssetId && x.ImageAssetId.HasValue))
            .WithErrorCode("Validation.Invalid")
            .WithMessage("ImageAssetId cannot be provided when ClearImageAssetId is true");
    }

    private void ConfigureStepRules() {
        RuleFor(x => x.Steps)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Validation.Required")
            .WithMessage("Steps are required")
            .Must(steps => steps is { Count: > 0 })
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Recipe must contain at least one step");

        RuleFor(x => x.Steps)
            .Must(static steps => HaveUniqueEffectiveStepOrder(steps!))
            .When(x => x.Steps is { Count: > 0 })
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Step order values must be unique");

        RuleForEach(x => x.Steps!)
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

    private static bool BeValidVisibility(string? visibility) =>
        visibility != null && SharedEnumValueParser.CanParse<Visibility>(visibility);

    private static bool HasManualNutrition(UpdateRecipeCommand command) =>
        command is { ManualCalories: not null, ManualProteins: not null, ManualFats: not null, ManualCarbs: not null, ManualFiber: not null };

    private static bool HaveUniqueEffectiveStepOrder(IReadOnlyList<RecipeStepInput> steps) {
        if (steps.Any(static step => step is null)) {
            return false;
        }

        var orders = new HashSet<int>();
        return steps.Select((step, index) => step.Order > 0 ? step.Order : index + 1).All(orders.Add);
    }
}
