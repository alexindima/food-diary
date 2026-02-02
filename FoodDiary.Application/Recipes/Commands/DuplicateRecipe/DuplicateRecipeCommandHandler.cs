using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Recipes.Mappings;
using FoodDiary.Contracts.Recipes;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.Enums;
using FoodDiary.Application.Recipes.Services;

namespace FoodDiary.Application.Recipes.Commands.DuplicateRecipe;

public class DuplicateRecipeCommandHandler(IRecipeRepository recipeRepository)
    : ICommandHandler<DuplicateRecipeCommand, Result<RecipeResponse>>
{
    public async Task<Result<RecipeResponse>> Handle(DuplicateRecipeCommand command, CancellationToken cancellationToken)
    {
        var original = await recipeRepository.GetByIdAsync(
            command.RecipeId,
            command.UserId!.Value,
            includePublic: true,
            includeSteps: true,
            cancellationToken: cancellationToken);

        if (original is null)
        {
            return Result.Failure<RecipeResponse>(Errors.Recipe.NotFound(command.RecipeId.Value));
        }

        var duplicate = Recipe.Create(
            command.UserId.Value,
            original.Name,
            original.Servings,
            original.Description,
            original.Comment,
            original.Category,
            original.ImageUrl,
            null,
            original.PrepTime,
            original.CookTime,
            original.Visibility);

        AddStepsFromOriginal(duplicate, original);

        if (original.IsNutritionAutoCalculated)
        {
            duplicate.EnableAutoNutrition();
        }
        else
        {
            duplicate.SetManualNutrition(
                original.ManualCalories ?? original.TotalCalories,
                original.ManualProteins ?? original.TotalProteins,
                original.ManualFats ?? original.TotalFats,
                original.ManualCarbs ?? original.TotalCarbs,
                original.ManualFiber ?? original.TotalFiber,
                original.ManualAlcohol ?? original.TotalAlcohol);
        }

        await recipeRepository.AddAsync(duplicate);

        var created = await recipeRepository.GetByIdAsync(
            duplicate.Id,
            command.UserId.Value,
            includePublic: false,
            includeSteps: true,
            asTracking: true,
            cancellationToken: cancellationToken);

        if (created is null)
        {
            return Result.Failure<RecipeResponse>(Errors.Recipe.InvalidData("Failed to load duplicated recipe."));
        }

        await RecipeNutritionUpdater.EnsureNutritionAsync(created, recipeRepository, cancellationToken);

        return Result.Success(created.ToResponse(0, true));
    }

    private static void AddStepsFromOriginal(Recipe target, Recipe source)
    {
        var orderedSteps = source.Steps
            .OrderBy(step => step.StepNumber)
            .ToList();

        foreach (var step in orderedSteps)
        {
            var newStep = target.AddStep(step.StepNumber, step.Instruction, step.ImageUrl);

            foreach (var ingredient in step.Ingredients)
            {
                if (ingredient.ProductId.HasValue)
                {
                    newStep.AddProductIngredient(ingredient.ProductId.Value, ingredient.Amount);
                }
                else if (ingredient.NestedRecipeId.HasValue)
                {
                    newStep.AddNestedRecipeIngredient(ingredient.NestedRecipeId.Value, ingredient.Amount);
                }
            }
        }
    }
}
