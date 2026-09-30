using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.MealPlanning.Application.MealPlans.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.MealPlanning.Application.MealPlans.Queries.GetCatalogMealPlans;

public sealed record GetCatalogMealPlansQuery(int Page = 1, int Limit = 20) : IQuery<Result<IReadOnlyList<MealPlanSummaryModel>>>;
