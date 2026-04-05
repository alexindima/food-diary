using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.MealPlans.Models;

namespace FoodDiary.Application.MealPlans.Queries.GetMealPlans;

public record GetMealPlansQuery(
    Guid? UserId,
    string? DietType) : IQuery<Result<IReadOnlyList<MealPlanSummaryModel>>>, IUserRequest;
