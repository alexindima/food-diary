namespace FoodDiary.Application.WeeklyGoals.Common;

internal static class WeeklyGoalWeekPolicy {
    public static bool CanWrite(DateOnly weekStart, DateTime utcNow) {
        DateOnly currentWeekStart = StartOfWeek(DateOnly.FromDateTime(utcNow));
        return weekStart >= currentWeekStart.AddDays(-7) && weekStart <= currentWeekStart.AddDays(7);
    }

    private static DateOnly StartOfWeek(DateOnly value) {
        int daysSinceMonday = ((int)value.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return value.AddDays(-daysSinceMonday);
    }
}
