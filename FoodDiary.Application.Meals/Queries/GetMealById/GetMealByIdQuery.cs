using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Meals.Models;

namespace FoodDiary.Application.Meals.Queries.GetMealById;

public record GetMealByIdQuery(Guid? UserId, Guid MealId)
    : IQuery<Result<MealModel>>, IUserRequest;
