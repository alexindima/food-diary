namespace FoodDiary.Application.Meals.Common;

public record MealItemInput(
    Guid? ProductId,
    Guid? RecipeId,
    double Amount,
    Guid? SourceAiItemId = null,
    string? Origin = null);
