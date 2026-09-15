namespace FoodDiary.Modules.Meals.Presentation.Requests;

public sealed record MealItemHttpRequest(
    Guid? ProductId,
    Guid? RecipeId,
    double Amount,
    Guid? SourceAiItemId = null,
    string? Origin = null);
