using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.MealPlanning.Application.MealPlans.Models;

namespace FoodDiary.Modules.MealPlanning.Application.MealPlans.Queries.GetMealPlanById;

public record GetMealPlanByIdQuery(
    Guid? UserId,
    Guid PlanId) : IQuery<Result<MealPlanModel>>, IUserRequest;
