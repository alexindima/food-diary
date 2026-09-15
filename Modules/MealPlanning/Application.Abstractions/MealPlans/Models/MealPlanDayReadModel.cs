namespace FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Models;

public sealed record MealPlanDayReadModel(
    Guid Id,
    int DayNumber,
    IReadOnlyList<MealPlanMealReadModel> Meals);
