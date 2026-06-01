namespace FoodDiary.Application.MealPlans.Models;

public sealed record MealPlanDayModel(
    Guid Id,
    int DayNumber,
    IReadOnlyList<MealPlanMealModel> Meals);
