using FoodDiary.Modules.Recipes.Application.Models;
using FoodDiary.Modules.Recipes.Presentation.Responses;
using FoodDiary.Application.Contracts.Common.Models;
using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Modules.Recipes.Presentation.Mappings;

public static class PublicRecipeHttpMappings {
    public static PublicRecipeHttpResponse ToHttpResponse(this PublicRecipeModel recipe) => new(
        recipe.Id, recipe.Name, recipe.Description, recipe.Category, recipe.ImageUrl, recipe.Images,
        recipe.PrepTime, recipe.CookTime, recipe.Servings, recipe.TotalCalories, recipe.TotalProteins,
        recipe.TotalFats, recipe.TotalCarbs, recipe.TotalFiber, recipe.TotalAlcohol, recipe.MissingIngredientCount,
        recipe.Steps.Select(step => new PublicRecipeStepHttpResponse(step.StepNumber, step.Title, step.Instruction,
            step.Images, step.Ingredients.Select(item => new PublicRecipeIngredientHttpResponse(item.Name, item.Amount,
                item.Unit, item.AmountText, item.RecipeId, item.IsAvailable)).ToArray())).ToArray()) { Language = recipe.Language };

    public static PagedHttpResponse<PublicRecipeHttpResponse> ToHttpResponse(this PagedResponse<PublicRecipeModel> page) =>
        new(page.Data.Select(item => item.ToHttpResponse()).ToArray(), page.Page, page.Limit, page.TotalPages, page.TotalItems);
}
