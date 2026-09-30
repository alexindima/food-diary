using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.MealPlanning.Application.MealPlans.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.MealPlanning.Application.MealPlans.Commands.SaveCatalogMealPlan;

public sealed record SaveCatalogMealPlanCommand(Guid? Id, string Name, string? Description, string DietType,
    int DurationDays, double? TargetCaloriesPerDay, bool IsPublished,
    IReadOnlyList<CatalogDayInput> Days) : ICommand<Result<MealPlanModel>>;
