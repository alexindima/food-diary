using FluentValidation;
using FluentValidation.Results;
using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Application.Common.Nutrition;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Recipes.Common;
using FoodDiary.Application.Recipes.Common.Validators;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Recipes.Commands.UpdateRecipe;

public sealed class UpdateRecipeCommandValidator : AbstractValidator<UpdateRecipeCommand> {
    private const string RecipeContextKey = "__recipe";
    private readonly IRecipeReadRepository _recipeRepository;

    public UpdateRecipeCommandValidator(IRecipeReadRepository recipeRepository) {
        _recipeRepository = recipeRepository;
        ConfigureIdentityRules();
        ConfigureBaseRecipeRules();
        ConfigureClearRules();
        ConfigureStepRules();
        ConfigureNutritionRules();

        RuleFor(x => x)
            .CustomAsync(EnsureRecipeEditableAsync);
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
    }

    private void ConfigureBaseRecipeRules() {
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

    private async Task EnsureRecipeEditableAsync(
        UpdateRecipeCommand command,
        ValidationContext<UpdateRecipeCommand> context,
        CancellationToken cancellationToken) {
        context.RootContextData.TryGetValue(RecipeContextKey, out object? cached);
        if (cached is Recipe recipe) {
            int cachedUsageCount = await _recipeRepository.GetUsageCountAsync(
                recipe.Id,
                recipe.UserId,
                includePublic: false,
                cancellationToken).ConfigureAwait(false);
            if (cachedUsageCount > 0) {
                context.AddFailure(new ValidationFailure(nameof(command.RecipeId),
                    "Recipe is already used and cannot be modified") {
                    ErrorCode = "Validation.Invalid",
                });
            }

            return;
        }

        if (command.UserId is null || command.UserId.Value == Guid.Empty) {
            return;
        }

        Recipe? existing = await _recipeRepository.GetByIdAsync(
            new RecipeId(command.RecipeId),
            new UserId(command.UserId.Value),
            includePublic: false,
            includeSteps: false,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (existing is null) {
            context.AddFailure(new ValidationFailure(nameof(command.RecipeId),
                "Recipe not found or you do not have permission to modify it") {
                ErrorCode = "Recipe.NotFound",
            });
            return;
        }

        context.RootContextData[RecipeContextKey] = existing;

        int usageCount = await _recipeRepository.GetUsageCountAsync(
            existing.Id,
            existing.UserId,
            includePublic: false,
            cancellationToken).ConfigureAwait(false);
        if (usageCount > 0) {
            context.AddFailure(new ValidationFailure(nameof(command.RecipeId),
                "Recipe is already used and cannot be modified") {
                ErrorCode = "Validation.Invalid",
            });
        }
    }

    private static bool BeValidVisibility(string? visibility) =>
        visibility != null && EnumValueParser.CanParse<Visibility>(visibility);

    private static bool HasManualNutrition(UpdateRecipeCommand command) =>
        command is { ManualCalories: not null, ManualProteins: not null, ManualFats: not null, ManualCarbs: not null, ManualFiber: not null };

    private static bool HaveUniqueEffectiveStepOrder(IReadOnlyList<RecipeStepInput> steps) {
        var orders = new HashSet<int>();
        return steps.Select((step, index) => step.Order > 0 ? step.Order : index + 1).All(orders.Add);
    }
}
