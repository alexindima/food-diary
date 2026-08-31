using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.MealPlanning.MealPlans.Models;

namespace FoodDiary.Application.MealPlanning.MealPlans.Commands.AdoptMealPlan;

public record AdoptMealPlanCommand(
    Guid? UserId,
    Guid PlanId) : ICommand<Result<MealPlanModel>>, IUserRequest;
