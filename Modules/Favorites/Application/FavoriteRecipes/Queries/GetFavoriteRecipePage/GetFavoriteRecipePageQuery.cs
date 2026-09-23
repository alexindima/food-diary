using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Modules.Favorites.Contracts.FavoriteRecipes.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Favorites.Application.FavoriteRecipes.Queries.GetFavoriteRecipePage;

public sealed record GetFavoriteRecipePageQuery(Guid? UserId, int Page = 1, int Limit = 10, string? Search = null)
    : IQuery<Result<PagedResponse<FavoriteRecipeModel>>>, IUserRequest;
