using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Meals.Service.Contracts.Models;

namespace FoodDiary.Modules.Meals.Application.Queries.GetMealById;

public record GetMealByIdQuery(Guid? UserId, Guid MealId)
    : IQuery<Result<MealModel>>, IUserRequest;
