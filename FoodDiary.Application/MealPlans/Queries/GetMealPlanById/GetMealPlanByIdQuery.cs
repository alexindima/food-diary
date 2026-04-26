using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.MealPlans.Models;

namespace FoodDiary.Application.MealPlans.Queries.GetMealPlanById;

public record GetMealPlanByIdQuery(
    Guid? UserId,
    Guid PlanId) : IQuery<Result<MealPlanModel>>, IUserRequest;
