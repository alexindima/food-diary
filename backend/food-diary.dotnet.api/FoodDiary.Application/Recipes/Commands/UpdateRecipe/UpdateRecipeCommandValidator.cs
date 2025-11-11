using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Recipes.Commands.Common;
using FoodDiary.Application.Recipes.Commands.Common.Validators;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Recipes.Commands.UpdateRecipe;

public class UpdateRecipeCommandValidator : AbstractValidator<UpdateRecipeCommand>
{
    private const string RecipeContextKey = "__recipe";
    private readonly IRecipeRepository _recipeRepository;

    public UpdateRecipeCommandValidator(IRecipeRepository recipeRepository)
    {
        _recipeRepository = recipeRepository;

        RuleFor(x => x.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .Must(id => id is not null && id.Value != UserId.Empty)
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
            .GreaterThan(0)
            .When(x => x.PrepTime.HasValue)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("PrepTime must be greater than zero");

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
    }

    private async Task EnsureRecipeEditableAsync(
        UpdateRecipeCommand command,
        ValidationContext<UpdateRecipeCommand> context,
        CancellationToken cancellationToken)
    {
        context.RootContextData.TryGetValue(RecipeContextKey, out var cached);
        if (cached is Recipe recipe)
        {
            if (!ValidateUsage(recipe))
            {
                context.AddFailure(new ValidationFailure(nameof(command.RecipeId),
                    "Recipe is already used and cannot be modified")
                {
                    ErrorCode = "Validation.Invalid"
                });
            }
            return;
        }

        if (command.UserId is null || command.UserId.Value == UserId.Empty)
        {
            return;
        }

        var existing = await _recipeRepository.GetByIdAsync(
            command.RecipeId,
            command.UserId.Value,
            includePublic: false,
            includeSteps: false,
            cancellationToken: cancellationToken);

        if (existing is null)
        {
            context.AddFailure(new ValidationFailure(nameof(command.RecipeId),
                "Recipe not found or you do not have permission to modify it")
            {
                ErrorCode = "Recipe.NotFound"
            });
            return;
        }

        context.RootContextData[RecipeContextKey] = existing;

        if (!ValidateUsage(existing))
        {
            context.AddFailure(new ValidationFailure(nameof(command.RecipeId),
                "Recipe is already used and cannot be modified")
            {
                ErrorCode = "Validation.Invalid"
            });
        }
    }

    private static bool ValidateUsage(Recipe recipe)
    {
        var usageCount = recipe.MealItems.Count + recipe.NestedRecipeUsages.Count;
        return usageCount == 0;
    }

    private static bool BeValidVisibility(string? visibility) =>
        visibility != null && Enum.TryParse(visibility, ignoreCase: true, out Visibility _);

    private static bool HasManualNutrition(UpdateRecipeCommand command) =>
        command.ManualCalories.HasValue
        && command.ManualProteins.HasValue
        && command.ManualFats.HasValue
        && command.ManualCarbs.HasValue
        && command.ManualFiber.HasValue;
}
