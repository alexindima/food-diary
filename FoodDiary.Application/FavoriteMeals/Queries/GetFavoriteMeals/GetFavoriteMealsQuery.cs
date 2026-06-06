using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.FavoriteMeals.Models;

namespace FoodDiary.Application.FavoriteMeals.Queries.GetFavoriteMeals;

public record GetFavoriteMealsQuery(
    Guid? UserId) : IQuery<Result<IReadOnlyList<FavoriteMealModel>>>, IUserRequest;
