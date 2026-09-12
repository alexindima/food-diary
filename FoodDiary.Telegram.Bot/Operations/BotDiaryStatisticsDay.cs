namespace FoodDiary.Telegram.Bot.Operations;

internal sealed record BotDiaryStatisticsDay(DateOnly Date, double Calories, double Proteins, double Fats,
    double Carbs, double Fiber, long WaterMl, int MealCount, double? CalorieGoal);
