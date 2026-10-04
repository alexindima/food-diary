using FoodDiary.Modules.Ai.Application.Commands.ImportRecipeVideo;
using FoodDiary.Modules.Ai.Application.Commands.ImportRecipe;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Modules.Ai.Presentation.Recipes.Requests;
using FoodDiary.Modules.Ai.Presentation.Responses;
using FoodDiary.Modules.Ai.Presentation.Requests;

namespace FoodDiary.Modules.Ai.Presentation.Recipes.Mappings;

public static class RecipeImportHttpMappings {
    public static ImportRecipeCommand ToCommand(this RecipeImportHttpRequest request, Guid userId, string requestId) =>
        new(userId, request.SourceUrl, request.Text, requestId);

    public static ImportRecipeVideoCommand ToCommand(this RecipeVideoImportHttpRequest request, Guid userId, Stream? video, string requestId) =>
        new(userId, video, request.SourceUrl, request.Text, requestId);

    public static RecipeImportHttpResponse ToHttpResponse(this RecipeImportDraftModel draft) => new(
        draft.Name, draft.Description, draft.Ingredients.Select(x => new RecipeImportIngredientHttpResponse(x.Name, x.Amount)).ToArray(),
        draft.Steps, draft.Servings, draft.PrepMinutes, draft.CookMinutes, draft.AuthorNutrition, draft.SourceUrl);
}
