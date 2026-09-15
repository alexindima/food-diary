using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Models;

namespace FoodDiary.Modules.Favorites.Application.FavoriteMeals.Queries.GetFavoriteMeals;

public record GetFavoriteMealsQuery(
    Guid? UserId) : IQuery<Result<IReadOnlyList<FavoriteMealModel>>>, IUserRequest;
