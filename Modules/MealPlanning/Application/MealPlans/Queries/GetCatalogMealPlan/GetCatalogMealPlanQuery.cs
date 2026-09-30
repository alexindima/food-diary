using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.MealPlanning.Application.MealPlans.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.MealPlanning.Application.MealPlans.Queries.GetCatalogMealPlan;

public sealed record GetCatalogMealPlanQuery(Guid Id) : IQuery<Result<MealPlanModel>>;
