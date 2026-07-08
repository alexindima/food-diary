using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Recipes.Mappings;
using FoodDiary.Application.Recipes.Models;
using FoodDiary.Application.Recipes.Services;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Recipes.Commands.DuplicateRecipe;

public sealed class DuplicateRecipeCommandHandler(
    IRecipeReadRepository recipeReadRepository,
    IRecipeWriteRepository recipeWriteRepository,
    IRecipeNutritionWriter recipeNutritionWriter,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<DuplicateRecipeCommand, Result<RecipeModel>> {
    public async Task<Result<RecipeModel>> Handle(DuplicateRecipeCommand command, CancellationToken cancellationToken) {
        Result<RecipeId> recipeIdResult = RequiredIdParser.Parse(
            command.RecipeId,
            nameof(command.RecipeId),
            "Recipe id must not be empty.",
            value => new RecipeId(value));
        if (recipeIdResult.IsFailure) {
            return RequiredIdParser.ToFailure<RecipeModel, RecipeId>(recipeIdResult);
        }

        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<RecipeModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        RecipeId recipeId = recipeIdResult.Value;

        Recipe? original = await recipeReadRepository.GetByIdAsync(
            recipeId,
            userId,
            includePublic: true,
            includeSteps: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (original is null) {
            return Result.Failure<RecipeModel>(Errors.Recipe.NotAccessible(command.RecipeId));
        }

        Recipe duplicate = CreateDuplicate(userId, original);
        await recipeWriteRepository.AddAsync(duplicate, cancellationToken).ConfigureAwait(false);
        await RecipeNutritionUpdater.EnsureNutritionAsync(duplicate, recipeNutritionWriter, cancellationToken).ConfigureAwait(false);

        return Result.Success(duplicate.ToModel(0, isOwnedByCurrentUser: true));
    }

    private static Recipe CreateDuplicate(UserId userId, Recipe original) {
        var duplicate = Recipe.Create(
            userId,
            original.Name,
            original.Servings,
            original.Description,
            original.Comment,
            original.Category,
            original.ImageUrl,
            imageAssetId: null,
            original.PrepTime,
            original.CookTime,
            original.Visibility);

        AddStepsFromOriginal(duplicate, original);
        ApplyNutritionSettings(duplicate, original);
        return duplicate;
    }

    private static void ApplyNutritionSettings(Recipe duplicate, Recipe original) {
        if (original.IsNutritionAutoCalculated) {
            duplicate.EnableAutoNutrition();
            duplicate.ApplyComputedNutrition(
                original.TotalCalories,
                original.TotalProteins,
                original.TotalFats,
                original.TotalCarbs,
                original.TotalFiber,
                original.TotalAlcohol);
            return;
        }

        duplicate.SetManualNutrition(
            original.ManualCalories ?? original.TotalCalories,
            original.ManualProteins ?? original.TotalProteins,
            original.ManualFats ?? original.TotalFats,
            original.ManualCarbs ?? original.TotalCarbs,
            original.ManualFiber ?? original.TotalFiber,
            original.ManualAlcohol ?? original.TotalAlcohol);
    }

    private static void AddStepsFromOriginal(Recipe target, Recipe source) {
        var orderedSteps = source.Steps
            .OrderBy(step => step.StepNumber)
            .ToList();

        foreach (RecipeStep step in orderedSteps) {
            RecipeStep newStep = target.AddStep(step.StepNumber, step.Instruction, step.Title, step.ImageUrl, step.ImageAssetId);

            foreach (RecipeIngredient ingredient in step.Ingredients) {
                if (ingredient.ProductId.HasValue) {
                    newStep.AddProductIngredient(ingredient.ProductId.Value, ingredient.Amount);
                } else if (ingredient.NestedRecipeId.HasValue) {
                    newStep.AddNestedRecipeIngredient(ingredient.NestedRecipeId.Value, ingredient.Amount);
                }
            }
        }
    }
}
