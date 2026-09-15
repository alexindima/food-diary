using FoodDiary.Modules.WeeklyGoals.Domain.Enums;
using FoodDiary.Modules.WeeklyGoals.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.WeeklyGoals.Application.Abstractions.Models;

public sealed record WeeklyGoalReadModel(
    WeeklyGoalId Id,
    DateTime WeekStartUtc,
    WeeklyGoalType Type,
    int TargetDays,
    bool ReminderEnabled,
    int? ReminderTimeMinutes,
    int? TimeZoneOffsetMinutes);
