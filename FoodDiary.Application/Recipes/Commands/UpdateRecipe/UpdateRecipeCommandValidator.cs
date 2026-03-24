using FluentValidation;
using FluentValidation.Results;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Recipes.Common;
using FoodDiary.Application.Recipes.Common.Validators;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Recipes.Commands.UpdateRecipe;

public class UpdateRecipeCommandValidator : AbstractValidator<UpdateRecipeCommand> {
    private const string RecipeContextKey = "__recipe";
    private readonly IRecipeRepository _recipeRepository;

    public UpdateRecipeCommandValidator(IRecipeRepository recipeRepository) {
        _recipeRepository = recipeRepository;

        RuleFor(x => x.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .Must(id => id is not null && id.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user");

        RuleFor(x => x.RecipeId)
            .Must(id => id != RecipeId.Empty)
            .WithErrorCode("Validation.Required")
            .WithMessage("RecipeId is required");

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

        RuleFor(x => x.Steps)
            .NotNull()
            .WithErrorCode("Validation.Required")
            .WithMessage("Steps are required")
            .Must(steps => steps is { Count: > 0 })
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Recipe must contain at least one step");

        RuleFor(x => x.Steps)
            .Must(HaveUniqueEffectiveStepOrder)
            .When(x => x.Steps is { Count: > 0 })
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Step order values must be unique");

        RuleForEach(x => x.Steps!)
            .SetValidator(new RecipeStepInputValidator());

        RuleFor(x => x)
            .CustomAsync(EnsureRecipeEditableAsync);

        RuleFor(x => x)
            .Must(cmd => cmd.CalculateNutritionAutomatically || HasManualNutrition(cmd))
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Manual nutrition values are required when automatic calculation is disabled.");

        RuleFor(x => x.ManualCalories)
            .GreaterThanOrEqualTo(0)
            .When(x => !x.CalculateNutritionAutomatically);

        RuleFor(x => x.ManualProteins)
            .GreaterThanOrEqualTo(0)
            .When(x => !x.CalculateNutritionAutomatically);

        RuleFor(x => x.ManualFats)
            .GreaterThanOrEqualTo(0)
            .When(x => !x.CalculateNutritionAutomatically);

        RuleFor(x => x.ManualCarbs)
            .GreaterThanOrEqualTo(0)
            .When(x => !x.CalculateNutritionAutomatically);

        RuleFor(x => x.ManualFiber)
            .GreaterThanOrEqualTo(0)
            .When(x => !x.CalculateNutritionAutomatically);

        RuleFor(x => x.ManualAlcohol)
            .GreaterThanOrEqualTo(0)
            .When(x => !x.CalculateNutritionAutomatically && x.ManualAlcohol.HasValue);
    }

    private async Task EnsureRecipeEditableAsync(
        UpdateRecipeCommand command,
        ValidationContext<UpdateRecipeCommand> context,
        CancellationToken cancellationToken) {
        context.RootContextData.TryGetValue(RecipeContextKey, out var cached);
        if (cached is Recipe recipe) {
            if (!ValidateUsage(recipe)) {
                context.AddFailure(new ValidationFailure(nameof(command.RecipeId),
                    "Recipe is already used and cannot be modified") {
                    ErrorCode = "Validation.Invalid"
                });
            }

            return;
        }

        if (command.UserId is null || command.UserId.Value == Guid.Empty) {
            return;
        }

        var existing = await _recipeRepository.GetByIdAsync(
            command.RecipeId,
            new UserId(command.UserId.Value),
            includePublic: false,
            includeSteps: false,
            cancellationToken: cancellationToken);

        if (existing is null) {
            context.AddFailure(new ValidationFailure(nameof(command.RecipeId),
                "Recipe not found or you do not have permission to modify it") {
                ErrorCode = "Recipe.NotFound"
            });
            return;
        }

        context.RootContextData[RecipeContextKey] = existing;

        if (!ValidateUsage(existing)) {
            context.AddFailure(new ValidationFailure(nameof(command.RecipeId),
                "Recipe is already used and cannot be modified") {
                ErrorCode = "Validation.Invalid"
            });
        }
    }

    private static bool ValidateUsage(Recipe recipe) {
        var usageCount = recipe.MealItems.Count + recipe.NestedRecipeUsages.Count;
        return usageCount == 0;
    }

    private static bool BeValidVisibility(string? visibility) =>
        visibility != null && Enum.TryParse(visibility, ignoreCase: true, out Visibility _);

    private static bool HasManualNutrition(UpdateRecipeCommand command) =>
        command is { ManualCalories: not null, ManualProteins: not null, ManualFats: not null, ManualCarbs: not null, ManualFiber: not null };

    private static bool HaveUniqueEffectiveStepOrder(IReadOnlyList<RecipeStepInput>? steps) {
        if (steps is null || steps.Count == 0) {
            return true;
        }

        var orders = new HashSet<int>();
        for (var index = 0; index < steps.Count; index++) {
            var step = steps[index];
            var effectiveOrder = step.Order > 0 ? step.Order : index + 1;
            if (!orders.Add(effectiveOrder)) {
                return false;
            }
        }

        return true;
    }
}
