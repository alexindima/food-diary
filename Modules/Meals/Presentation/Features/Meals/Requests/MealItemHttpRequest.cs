namespace FoodDiary.Presentation.Api.Features.Meals.Requests;

public sealed record MealItemHttpRequest(
    Guid? ProductId,
    Guid? RecipeId,
    double Amount,
    Guid? SourceAiItemId = null,
    string? Origin = null);
