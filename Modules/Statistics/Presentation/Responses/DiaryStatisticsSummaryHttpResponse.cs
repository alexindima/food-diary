namespace FoodDiary.Modules.Statistics.Presentation.Responses;

public sealed record DiaryStatisticsSummaryHttpResponse(string TimeZoneId, int CalendarDays, int DaysWithMeals,
    double TotalCalories, double TotalProteins, double TotalFats, double TotalCarbs, double TotalFiber, long TotalWaterMl,
    int MealCount, double AverageCaloriesPerCalendarDay, double? DailyWaterGoalMl, IReadOnlyList<DiaryStatisticsDayHttpResponse> Days);
