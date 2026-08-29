namespace FoodDiary.Application.WeeklyGoals.Models;

public sealed record WeeklyGoalModel(
    Guid Id,
    DateOnly WeekStart,
    string Type,
    int TargetDays,
    int ProgressDays,
    bool IsCompleted,
    bool ReminderEnabled,
    TimeOnly? ReminderTime,
    int? TimeZoneOffsetMinutes);
