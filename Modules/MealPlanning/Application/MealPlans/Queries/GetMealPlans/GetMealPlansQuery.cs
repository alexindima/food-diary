using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.MealPlanning.Application.MealPlans.Models;

namespace FoodDiary.Modules.MealPlanning.Application.MealPlans.Queries.GetMealPlans;

public record GetMealPlansQuery(
    Guid? UserId,
    string? DietType) : IQuery<Result<IReadOnlyList<MealPlanSummaryModel>>>, IUserRequest;
