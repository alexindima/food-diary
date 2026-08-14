using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.MealPlanning.MealPlans.Models;

namespace FoodDiary.Application.MealPlanning.MealPlans.Queries.GetMealPlans;

public record GetMealPlansQuery(
    Guid? UserId,
    string? DietType) : IQuery<Result<IReadOnlyList<MealPlanSummaryModel>>>, IUserRequest;
