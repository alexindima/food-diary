using System.ComponentModel.DataAnnotations;

namespace FoodDiary.Modules.WeeklyGoals.Presentation.Requests;

public sealed record UpsertWeeklyGoalHttpRequest(
    DateOnly WeekStart,
    int TargetDays,
    bool ReminderEnabled,
    TimeOnly? ReminderTime,
    int? TimeZoneOffsetMinutes, [MaxLength(100)] string? TimeZoneId = null);
