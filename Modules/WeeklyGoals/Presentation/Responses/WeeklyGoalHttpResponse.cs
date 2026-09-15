namespace FoodDiary.Modules.WeeklyGoals.Presentation.Responses;

public sealed record WeeklyGoalHttpResponse(
    Guid Id,
    DateOnly WeekStart,
    string Type,
    int TargetDays,
    int ProgressDays,
    bool IsCompleted,
    bool ReminderEnabled,
    TimeOnly? ReminderTime,
    int? TimeZoneOffsetMinutes);
