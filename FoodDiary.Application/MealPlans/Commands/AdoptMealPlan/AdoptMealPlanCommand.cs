using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.MealPlans.Models;

namespace FoodDiary.Application.MealPlans.Commands.AdoptMealPlan;

public record AdoptMealPlanCommand(
    Guid? UserId,
    Guid PlanId) : ICommand<Result<MealPlanModel>>, IUserRequest;
