using FoodDiary.Modules.WeeklyGoals.Contracts.Models;
using FoodDiary.Modules.WeeklyGoals.Domain.Entities;
using FoodDiary.Modules.WeeklyGoals.Application.Abstractions.Models;

namespace FoodDiary.Modules.WeeklyGoals.Application.Common;

internal static class WeeklyGoalMapping {
    public static WeeklyGoalModel ToModel(this WeeklyGoal goal, int progressDays) =>
        new WeeklyGoalReadModel(goal.Id, goal.WeekStartUtc, goal.Type, goal.TargetDays,
            goal.ReminderEnabled, goal.ReminderTimeMinutes, goal.TimeZoneOffsetMinutes).ToModel(progressDays);

    public static WeeklyGoalModel ToModel(this WeeklyGoalReadModel goal, int progressDays) {
        TimeOnly? reminderTime = goal.ReminderTimeMinutes is { } minutes
            ? TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(minutes))
            : null;
        return new WeeklyGoalModel(
            goal.Id.Value,
            DateOnly.FromDateTime(goal.WeekStartUtc),
            goal.Type.ToString(),
            goal.TargetDays,
            progressDays,
            progressDays >= goal.TargetDays,
            goal.ReminderEnabled,
            reminderTime,
            goal.TimeZoneOffsetMinutes);
    }
}
