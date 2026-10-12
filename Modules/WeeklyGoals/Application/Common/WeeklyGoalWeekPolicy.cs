using FoodDiary.Application.Contracts.Common.Validation;

namespace FoodDiary.Modules.WeeklyGoals.Application.Common;

internal static class WeeklyGoalWeekPolicy {
    public static bool CanWrite(DateOnly weekStart, DateTime utcNow, TimeZoneInfo? timeZone = null) {
        DateOnly today = timeZone is null ? DateOnly.FromDateTime(utcNow) : LocalCalendar.DateAt(utcNow, timeZone);
        DateOnly currentWeekStart = StartOfWeek(today);
        return weekStart >= currentWeekStart.AddDays(-7) && weekStart <= currentWeekStart.AddDays(7);
    }

    private static DateOnly StartOfWeek(DateOnly value) {
        int daysSinceMonday = ((int)value.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return value.AddDays(-daysSinceMonday);
    }
}
