using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.FavoriteRecipes.Models;

namespace FoodDiary.Application.Favorites.FavoriteRecipes.Queries.GetFavoriteRecipes;

public record GetFavoriteRecipesQuery(
    Guid? UserId) : IQuery<Result<IReadOnlyList<FavoriteRecipeModel>>>, IUserRequest;
