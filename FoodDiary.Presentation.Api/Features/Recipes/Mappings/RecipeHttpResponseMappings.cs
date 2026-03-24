using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Recipes.Models;
using FoodDiary.Presentation.Api.Features.Recipes.Responses;

namespace FoodDiary.Presentation.Api.Features.Recipes.Mappings;

public static class RecipeHttpResponseMappings {
    public static RecipeHttpResponse ToHttpResponse(this RecipeModel model) {
        return new RecipeHttpResponse(
            model.Id,
            model.Name,
            model.Description,
            model.Comment,
            model.Category,
            model.ImageUrl,
            model.ImageAssetId,
            model.PrepTime,
            model.CookTime,
            model.Servings,
            model.TotalCalories,
            model.TotalProteins,
            model.TotalFats,
            model.TotalCarbs,
            model.TotalFiber,
            model.TotalAlcohol,
            model.IsNutritionAutoCalculated,
            model.ManualCalories,
            model.ManualProteins,
            model.ManualFats,
            model.ManualCarbs,
            model.ManualFiber,
            model.ManualAlcohol,
            model.Visibility,
            model.UsageCount,
            model.CreatedAt,
            model.IsOwnedByCurrentUser,
            model.Steps.Select(ToHttpResponse).ToList()
        );
    }

    public static RecipeListWithRecentHttpResponse ToHttpResponse(this RecipeListWithRecentModel model) {
        return new RecipeListWithRecentHttpResponse(
            model.RecentItems.Select(ToHttpResponse).ToList(),
            model.AllRecipes.ToHttpResponse()
        );
    }

    public static PagedResponse<RecipeHttpResponse> ToHttpResponse(this PagedResponse<RecipeModel> response) {
        return new PagedResponse<RecipeHttpResponse>(
            response.Data.Select(ToHttpResponse).ToList(),
            response.Page,
            response.Limit,
            response.TotalPages,
            response.TotalItems
        );
    }

    private static RecipeStepHttpResponse ToHttpResponse(this RecipeStepModel model) {
        return new RecipeStepHttpResponse(
            model.Id,
            model.StepNumber,
            model.Title,
            model.Instruction,
            model.ImageUrl,
            model.ImageAssetId,
            model.Ingredients.Select(ToHttpResponse).ToList()
        );
    }

    private static RecipeIngredientHttpResponse ToHttpResponse(this RecipeIngredientModel model) {
        return new RecipeIngredientHttpResponse(
            model.Id,
            model.Amount,
            model.ProductId,
            model.ProductName,
            model.ProductBaseUnit,
            model.ProductBaseAmount,
            model.ProductCaloriesPerBase,
            model.ProductProteinsPerBase,
            model.ProductFatsPerBase,
            model.ProductCarbsPerBase,
            model.ProductFiberPerBase,
            model.ProductAlcoholPerBase,
            model.NestedRecipeId,
            model.NestedRecipeName
        );
    }
}
