using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.MealPlanning.Application.MealPlans.Commands.DeleteMealPlan;

public record DeleteMealPlanCommand(Guid? UserId, Guid PlanId) : ICommand<Result>, IUserRequest;
