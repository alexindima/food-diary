using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.FavoriteMeals.Models;

namespace FoodDiary.Application.FavoriteMeals.Queries.GetFavoriteMeals;

public record GetFavoriteMealsQuery(
    Guid? UserId) : IQuery<Result<IReadOnlyList<FavoriteMealModel>>>, IUserRequest;
