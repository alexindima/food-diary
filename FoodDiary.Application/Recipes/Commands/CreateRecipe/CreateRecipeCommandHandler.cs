using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Recipes.Mappings;
using FoodDiary.Contracts.Recipes;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Application.Recipes.Services;

namespace FoodDiary.Application.Recipes.Commands.CreateRecipe;

public class CreateRecipeCommandHandler(IRecipeRepository recipeRepository)
    : ICommandHandler<CreateRecipeCommand, Result<RecipeResponse>>
{
    public async Task<Result<RecipeResponse>> Handle(CreateRecipeCommand command, CancellationToken cancellationToken)
    {
        var userId = command.UserId!.Value;

        var visibility = Enum.Parse<Visibility>(command.Visibility, true);
        var recipe = Recipe.Create(
            userId,
            command.Name,
            command.Servings,
            command.Description,
            command.Comment,
            command.Category,
            command.ImageUrl,
            command.ImageAssetId.HasValue ? new ImageAssetId(command.ImageAssetId.Value) : null,
            command.PrepTime ?? 0,
            command.CookTime,
            visibility);

        AddSteps(recipe, command);

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
                command.ManualFiber ?? 0,
                command.ManualAlcohol ?? 0);
        }

        await recipeRepository.AddAsync(recipe);

        var created = await recipeRepository.GetByIdAsync(
            recipe.Id,
            userId,
            includePublic: false,
            includeSteps: true,
            asTracking: true,
            cancellationToken: cancellationToken);

        if (created is null)
        {
            return Result.Failure<RecipeResponse>(Errors.Recipe.InvalidData("Failed to load created recipe."));
        }

        await RecipeNutritionUpdater.EnsureNutritionAsync(created, recipeRepository, cancellationToken);

        return Result.Success(created.ToResponse(0, true));
    }

    private static void AddSteps(Recipe recipe, CreateRecipeCommand command)
    {
        var orderedSteps = command.Steps
            .Select((step, index) => new { Step = step, Order = step.Order > 0 ? step.Order : index + 1 })
            .OrderBy(x => x.Order);

        foreach (var entry in orderedSteps)
        {
            var step = recipe.AddStep(
                entry.Order,
                entry.Step.Description,
                entry.Step.Title,
                entry.Step.ImageUrl,
                entry.Step.ImageAssetId.HasValue ? new ImageAssetId(entry.Step.ImageAssetId.Value) : null);
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
    }
}
