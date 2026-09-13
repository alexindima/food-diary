namespace FoodDiary.Presentation.Api.Features.FavoriteMeals.Responses;

public sealed record FavoriteMealHttpResponse(
    Guid Id,
    Guid MealId,
    string? Name,
    DateTime CreatedAtUtc,
    DateTime MealDate,
    string? MealType,
    double TotalCalories,
    double TotalProteins,
    double TotalFats,
    double TotalCarbs,
    int ItemCount);
