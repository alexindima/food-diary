using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Recipes.Commands.Common;
using FoodDiary.Application.Recipes.Mappings;
using FoodDiary.Contracts.Recipes;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Application.Recipes.Services;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Common.Interfaces.Persistence;

namespace FoodDiary.Application.Recipes.Commands.UpdateRecipe;

public class UpdateRecipeCommandHandler(
    IRecipeRepository recipeRepository,
    IImageAssetRepository imageAssetRepository,
    IImageStorageService imageStorageService)
    : ICommandHandler<UpdateRecipeCommand, Result<RecipeResponse>>
{
    public async Task<Result<RecipeResponse>> Handle(UpdateRecipeCommand command, CancellationToken cancellationToken)
    {
        var userId = command.UserId!.Value;

        var recipe = await recipeRepository.GetByIdAsync(
            command.RecipeId,
            userId,
            includePublic: false,
            includeSteps: true,
            asTracking: true,
            cancellationToken: cancellationToken);

        if (recipe is null)
        {
            return Result.Failure<RecipeResponse>(Errors.Recipe.NotAccessible(command.RecipeId.Value));
        }

        var usageCount = recipe.MealItems.Count + recipe.NestedRecipeUsages.Count;
        if (usageCount > 0)
        {
            return Result.Failure<RecipeResponse>(
                Errors.Validation.Invalid("RecipeId", "Recipe is already used and cannot be modified"));
        }

        Visibility? visibility = null;
        if (!string.IsNullOrWhiteSpace(command.Visibility))
        {
            visibility = Enum.Parse<Visibility>(command.Visibility, true);
        }

        var oldAssetId = recipe.ImageAssetId;

        recipe.Update(
            name: command.Name,
            description: command.Description,
            category: command.Category,
            imageUrl: command.ImageUrl,
            imageAssetId: command.ImageAssetId.HasValue ? new ImageAssetId(command.ImageAssetId.Value) : null,
            prepTime: command.PrepTime,
            cookTime: command.CookTime,
            servings: command.Servings,
            visibility: visibility);

        recipe.ClearSteps();
        var steps = command.Steps ?? Array.Empty<Application.Recipes.Commands.Common.RecipeStepInput>();
        var orderedSteps = steps
            .Select((step, index) => new { Step = step, Order = step.Order > 0 ? step.Order : index + 1 })
            .OrderBy(x => x.Order);

        foreach (var entry in orderedSteps)
        {
            var step = recipe.AddStep(entry.Order, entry.Step.Description, entry.Step.ImageUrl);
            foreach (var ingredient in entry.Step.Ingredients)
            {
                if (ingredient.ProductId.HasValue)
                {
                    step.AddProductIngredient(new ProductId(ingredient.ProductId.Value), ingredient.Amount);
                }
                else if (ingredient.NestedRecipeId.HasValue)
                {
                    step.AddNestedRecipeIngredient(new RecipeId(ingredient.NestedRecipeId.Value), ingredient.Amount);
                }
            }
        }

        if (command.CalculateNutritionAutomatically)
        {
            recipe.EnableAutoNutrition();
        }
        else
        {
            recipe.SetManualNutrition(
                command.ManualCalories ?? 0,
                command.ManualProteins ?? 0,
                command.ManualFats ?? 0,
                command.ManualCarbs ?? 0,
                command.ManualFiber ?? 0);
        }

        await recipeRepository.UpdateAsync(recipe);

        var updated = await recipeRepository.GetByIdAsync(
            recipe.Id,
            userId,
            includePublic: false,
            includeSteps: true,
            asTracking: true,
            cancellationToken: cancellationToken);

        if (updated is null)
        {
            return Result.Failure<RecipeResponse>(Errors.Recipe.InvalidData("Failed to load updated recipe."));
        }

        await RecipeNutritionUpdater.EnsureNutritionAsync(updated, recipeRepository, cancellationToken);

        if (oldAssetId.HasValue && (!command.ImageAssetId.HasValue || oldAssetId.Value.Value != command.ImageAssetId.Value))
        {
            await TryDeleteAssetAsync(oldAssetId.Value, imageAssetRepository, imageStorageService, cancellationToken);
        }

        return Result.Success(updated.ToResponse(updated.MealItems.Count + updated.NestedRecipeUsages.Count, true));
    }

    private static async Task TryDeleteAssetAsync(
        ImageAssetId assetId,
        IImageAssetRepository imageAssetRepository,
        IImageStorageService storageService,
        CancellationToken cancellationToken)
    {
        var asset = await imageAssetRepository.GetByIdAsync(assetId, cancellationToken);
        if (asset is null)
        {
            return;
        }

        var inUse = await imageAssetRepository.IsAssetInUse(assetId, cancellationToken);
        if (inUse)
        {
            return;
        }

        await storageService.DeleteAsync(asset.ObjectKey, cancellationToken);
        await imageAssetRepository.DeleteAsync(asset, cancellationToken);
    }
}
