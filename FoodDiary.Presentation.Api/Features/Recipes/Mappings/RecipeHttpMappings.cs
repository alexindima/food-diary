using FoodDiary.Application.Recipes.Commands.CreateRecipe;
using FoodDiary.Application.Recipes.Commands.DeleteRecipe;
using FoodDiary.Application.Recipes.Commands.DuplicateRecipe;
using FoodDiary.Application.Recipes.Commands.UpdateRecipe;
using FoodDiary.Application.Recipes.Common;
using FoodDiary.Presentation.Api.Features.Recipes.Requests;

namespace FoodDiary.Presentation.Api.Features.Recipes.Mappings;

public static class RecipeHttpMappings {
    public static DeleteRecipeCommand ToDeleteCommand(this Guid recipeId, Guid userId) =>
        new(userId, recipeId);

    public static DuplicateRecipeCommand ToDuplicateCommand(this Guid recipeId, Guid userId) =>
        new(userId, recipeId);

    public static CreateRecipeCommand ToCommand(this CreateRecipeHttpRequest request, Guid userIdValue) {
        return new CreateRecipeCommand(
            UserId: userIdValue,
            Name: request.Name,
            Description: request.Description,
            Comment: request.Comment,
            Category: request.Category,
            ImageUrl: request.ImageUrl,
            ImageAssetId: request.ImageAssetId,
            PrepTime: request.PrepTime,
            CookTime: request.CookTime,
            Servings: request.Servings,
            Visibility: request.Visibility,
            CalculateNutritionAutomatically: request.CalculateNutritionAutomatically,
            ManualCalories: request.ManualCalories,
            ManualProteins: request.ManualProteins,
            ManualFats: request.ManualFats,
            ManualCarbs: request.ManualCarbs,
            ManualFiber: request.ManualFiber,
            ManualAlcohol: request.ManualAlcohol,
            Steps: MapSteps(request.Steps));
    }

    public static UpdateRecipeCommand ToCommand(this UpdateRecipeHttpRequest request, Guid userIdValue, Guid recipeId) {
        return new UpdateRecipeCommand(
            UserId: userIdValue,
            RecipeId: recipeId,
            Name: request.Name,
            Description: request.Description,
            Comment: request.Comment,
            Category: request.Category,
            ImageUrl: request.ImageUrl,
            ImageAssetId: request.ImageAssetId,
            PrepTime: request.PrepTime,
            CookTime: request.CookTime,
            Servings: request.Servings,
            Visibility: request.Visibility,
            CalculateNutritionAutomatically: request.CalculateNutritionAutomatically,
            ManualCalories: request.ManualCalories,
            ManualProteins: request.ManualProteins,
            ManualFats: request.ManualFats,
            ManualCarbs: request.ManualCarbs,
            ManualFiber: request.ManualFiber,
            ManualAlcohol: request.ManualAlcohol,
            Steps: request.Steps is null ? null : MapSteps(request.Steps));
    }

    private static IReadOnlyList<RecipeStepInput> MapSteps(IReadOnlyList<RecipeStepHttpRequest> steps) =>
        steps.Select((step, index) =>
                new RecipeStepInput(
                    index + 1,
                    step.Description,
                    step.Title,
                    step.ImageUrl,
                    step.ImageAssetId,
                    step.Ingredients
                        .Select(ingredient => new RecipeIngredientInput(
                            ingredient.ProductId,
                            ingredient.NestedRecipeId,
                            ingredient.Amount))
                        .ToList()))
            .ToList();
}
