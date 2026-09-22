namespace FoodDiary.Modules.Meals.Application.Abstractions.Models;

public sealed record MealDaySummary(DateOnly Date, double TotalCalories, int MealCount);
