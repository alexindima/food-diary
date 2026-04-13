namespace FoodDiary.Presentation.Api.Features.FavoriteRecipes.Requests;

public sealed record AddFavoriteRecipeHttpRequest(Guid RecipeId, string? Name = null);
