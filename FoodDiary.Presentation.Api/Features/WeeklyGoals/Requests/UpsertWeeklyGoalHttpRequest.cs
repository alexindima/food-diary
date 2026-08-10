namespace FoodDiary.Presentation.Api.Features.WeeklyGoals.Requests;

public sealed record UpsertWeeklyGoalHttpRequest(
    DateOnly WeekStart,
    int TargetDays,
    bool ReminderEnabled,
    TimeOnly? ReminderTime,
    int? TimeZoneOffsetMinutes);
