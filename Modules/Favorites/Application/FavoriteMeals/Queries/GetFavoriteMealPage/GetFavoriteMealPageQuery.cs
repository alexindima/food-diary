using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Favorites.Application.FavoriteMeals.Queries.GetFavoriteMealPage;

public sealed record GetFavoriteMealPageQuery(Guid? UserId, int Page = 1, int Limit = 10, string? Search = null)
    : IQuery<Result<PagedResponse<FavoriteMealModel>>>, IUserRequest;
