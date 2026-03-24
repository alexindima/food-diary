using FoodDiary.Application.Recipes.Commands.CreateRecipe;
using FoodDiary.Application.Recipes.Commands.UpdateRecipe;
using FoodDiary.Application.Recipes.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Presentation.Api.Features.Recipes.Requests;

namespace FoodDiary.Presentation.Api.Features.Recipes.Mappings;

public static class RecipeHttpMappings {
    public static CreateRecipeCommand ToCommand(this CreateRecipeHttpRequest request, Guid userIdValue) {
        return new CreateRecipeCommand(
            new UserId(userIdValue),
            request.Name,
            request.Description,
            request.Comment,
            request.Category,
            request.ImageUrl,
            request.ImageAssetId,
            request.PrepTime,
            request.CookTime,
            request.Servings,
            request.Visibility,
            request.CalculateNutritionAutomatically,
            request.ManualCalories,
            request.ManualProteins,
            request.ManualFats,
            request.ManualCarbs,
            request.ManualFiber,
            request.ManualAlcohol,
            MapSteps(request.Steps));
    }

    public static UpdateRecipeCommand ToCommand(this UpdateRecipeHttpRequest request, Guid userIdValue, Guid recipeId) {
        return new UpdateRecipeCommand(
            new UserId(userIdValue),
            new RecipeId(recipeId),
            request.Name,
            request.Description,
            request.Comment,
            request.Category,
            request.ImageUrl,
            request.ImageAssetId,
            request.PrepTime,
            request.CookTime,
            request.Servings,
            request.Visibility,
            request.CalculateNutritionAutomatically,
            request.ManualCalories,
            request.ManualProteins,
            request.ManualFats,
            request.ManualCarbs,
            request.ManualFiber,
            request.ManualAlcohol,
            request.Steps is null ? null : MapSteps(request.Steps));
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
