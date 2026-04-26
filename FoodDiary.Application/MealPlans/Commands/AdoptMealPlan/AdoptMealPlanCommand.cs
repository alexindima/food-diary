using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.MealPlans.Models;

namespace FoodDiary.Application.MealPlans.Commands.AdoptMealPlan;

public record AdoptMealPlanCommand(
    Guid? UserId,
    Guid PlanId) : ICommand<Result<MealPlanModel>>, IUserRequest;
