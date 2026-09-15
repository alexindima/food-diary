namespace FoodDiary.Modules.WeeklyGoals.Presentation.Requests;

public sealed record UpsertWeeklyGoalHttpRequest(
    DateOnly WeekStart,
    int TargetDays,
    bool ReminderEnabled,
    TimeOnly? ReminderTime,
    int? TimeZoneOffsetMinutes);
