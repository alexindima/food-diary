namespace FoodDiary.Modules.Favorites.Presentation.Features.FavoriteMeals.Requests;

public sealed record AddFavoriteMealHttpRequest(Guid MealId, string? Name = null);
