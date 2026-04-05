namespace FoodDiary.Presentation.Api.Features.FavoriteMeals.Requests;

public sealed record AddFavoriteMealHttpRequest(Guid MealId, string? Name = null);
