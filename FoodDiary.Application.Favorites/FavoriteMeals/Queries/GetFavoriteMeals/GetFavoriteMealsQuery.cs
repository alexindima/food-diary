using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.FavoriteMeals.Models;

namespace FoodDiary.Application.Favorites.FavoriteMeals.Queries.GetFavoriteMeals;

public record GetFavoriteMealsQuery(
    Guid? UserId) : IQuery<Result<IReadOnlyList<FavoriteMealModel>>>, IUserRequest;
