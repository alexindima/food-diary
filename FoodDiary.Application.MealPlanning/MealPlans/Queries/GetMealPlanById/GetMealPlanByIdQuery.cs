using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.MealPlanning.MealPlans.Models;

namespace FoodDiary.Application.MealPlanning.MealPlans.Queries.GetMealPlanById;

public record GetMealPlanByIdQuery(
    Guid? UserId,
    Guid PlanId) : IQuery<Result<MealPlanModel>>, IUserRequest;
