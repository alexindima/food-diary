namespace FoodDiary.Application.Statistics.Models;

public sealed record DiaryStatisticsDayModel(DateOnly Date, double Calories, double Proteins, double Fats, double Carbs,
    double Fiber, long WaterMl, int MealCount, double? CalorieGoal);
