using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.MealPlanning.Application.MealPlans.Models;
using FoodDiary.Application.Contracts.Common.Models;

namespace FoodDiary.Modules.MealPlanning.Application.MealPlans.Queries.GetMealPlans;

public record GetMealPlansQuery(
    Guid? UserId,
    string? DietType,
    int Page = 1,
    int Limit = 20) : IQuery<Result<PagedResponse<MealPlanSummaryModel>>>, IUserRequest;
