using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Favorites.Contracts.FavoriteRecipes.Models;

namespace FoodDiary.Modules.Favorites.Application.FavoriteRecipes.Queries.GetFavoriteRecipes;

public record GetFavoriteRecipesQuery(
    Guid? UserId) : IQuery<Result<IReadOnlyList<FavoriteRecipeModel>>>, IUserRequest;
