using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.FavoriteRecipes.Models;

namespace FoodDiary.Application.FavoriteRecipes.Queries.GetFavoriteRecipes;

public record GetFavoriteRecipesQuery(
    Guid? UserId) : IQuery<Result<IReadOnlyList<FavoriteRecipeModel>>>, IUserRequest;
