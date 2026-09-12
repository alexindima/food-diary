namespace FoodDiary.Telegram.Bot.Operations;

internal sealed record BotDiaryStatistics(string TimeZoneId, int CalendarDays, int DaysWithMeals,
    double TotalCalories, double TotalProteins, double TotalFats, double TotalCarbs, double TotalFiber,
    long TotalWaterMl, int MealCount, double AverageCaloriesPerCalendarDay, double? DailyWaterGoalMl,
    IReadOnlyList<BotDiaryStatisticsDay> Days);
