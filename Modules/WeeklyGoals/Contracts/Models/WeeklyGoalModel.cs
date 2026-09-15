namespace FoodDiary.Modules.WeeklyGoals.Contracts.Models;

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
