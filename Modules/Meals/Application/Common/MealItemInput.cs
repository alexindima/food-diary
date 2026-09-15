namespace FoodDiary.Modules.Meals.Application.Common;

public record MealItemInput(
    Guid? ProductId,
    Guid? RecipeId,
    double Amount,
    Guid? SourceAiItemId = null,
    string? Origin = null);
