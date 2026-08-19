using FoodDiary.Application.Recipes.Recipes.Commands.CreateRecipe;
using FoodDiary.Application.Recipes.Recipes.Commands.DeleteRecipe;
using FoodDiary.Application.Recipes.Recipes.Commands.DuplicateRecipe;
using FoodDiary.Application.Recipes.Recipes.Commands.UpdateRecipe;
using FoodDiary.Application.Recipes.Recipes.Common;
using FoodDiary.Presentation.Api.Features.Recipes.Requests;

namespace FoodDiary.Presentation.Api.Features.Recipes.Mappings;

public static class RecipeHttpMappings {
    extension(Guid recipeId) {
        public DeleteRecipeCommand ToDeleteCommand(Guid userId) =>
            new(userId, recipeId);
        public DuplicateRecipeCommand ToDuplicateCommand(Guid userId) =>
            new(userId, recipeId);
    }

    extension(CreateRecipeHttpRequest request) {
        public CreateRecipeCommand ToCommand(Guid userIdValue) {
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
    }

    extension(UpdateRecipeHttpRequest request) {
        public UpdateRecipeCommand ToCommand(Guid userIdValue, Guid recipeId) {
            return new UpdateRecipeCommand(
                UserId: userIdValue,
                RecipeId: recipeId,
                Name: request.Name,
                Description: request.Description,
                ClearDescription: request.ClearDescription,
                Comment: request.Comment,
                ClearComment: request.ClearComment,
                Category: request.Category,
                ClearCategory: request.ClearCategory,
                ImageUrl: request.ImageUrl,
                ClearImageUrl: request.ClearImageUrl,
                ImageAssetId: request.ImageAssetId,
                ClearImageAssetId: request.ClearImageAssetId,
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
    }

    private static IReadOnlyList<RecipeStepInput> MapSteps(IReadOnlyList<RecipeStepHttpRequest> steps) =>
        steps.Select((step, index) =>
                step is null
                    ? null!
                    : new RecipeStepInput(
                        index + 1,
                        step.Description,
                        step.Title,
                        step.ImageUrl,
                        step.ImageAssetId,
                        step.Ingredients?
                            .Select(ingredient => ingredient is null
                                ? null!
                                : new RecipeIngredientInput(
                                    ingredient.ProductId,
                                    ingredient.NestedRecipeId,
                                    ingredient.Amount))
                            .ToList()!))
            .ToList();
}
