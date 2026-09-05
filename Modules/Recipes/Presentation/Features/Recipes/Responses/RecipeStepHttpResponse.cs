namespace FoodDiary.Presentation.Api.Features.Recipes.Responses;

public sealed record RecipeStepHttpResponse(
    Guid Id,
    int StepNumber,
    string? Title,
    string Instruction,
    string? ImageUrl,
    Guid? ImageAssetId,
    IReadOnlyList<RecipeIngredientHttpResponse> Ingredients);
