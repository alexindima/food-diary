namespace FoodDiary.Application.Dashboard.Models;

public sealed record DailyCaloriesModel(
    DateTime Date,
    double Calories,
    double Proteins = 0,
    double Fats = 0,
    double Carbs = 0,
    double Fiber = 0);
