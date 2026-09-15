namespace FoodDiary.Modules.Favorites.Presentation.Features.FavoriteRecipes.Requests;

public sealed record AddFavoriteRecipeHttpRequest(Guid RecipeId, string? Name = null);
