using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.MealPlanning.Application.MealPlans.Models;

namespace FoodDiary.Modules.MealPlanning.Application.MealPlans.Commands.AdoptMealPlan;

public record AdoptMealPlanCommand(
    Guid? UserId,
    Guid PlanId) : ICommand<Result<MealPlanModel>>, IUserRequest;
