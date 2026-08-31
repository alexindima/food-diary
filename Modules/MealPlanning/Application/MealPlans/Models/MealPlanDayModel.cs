namespace FoodDiary.Application.MealPlanning.MealPlans.Models;

public sealed record MealPlanDayModel(
    Guid Id,
    int DayNumber,
    IReadOnlyList<MealPlanMealModel> Meals);
