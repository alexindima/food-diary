using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.WeeklyGoals.Domain.Entities;
using FoodDiary.Modules.WeeklyGoals.Domain.Enums;
using FoodDiary.Modules.WeeklyGoals.Domain.ValueObjects;

namespace FoodDiary.Modules.WeeklyGoals.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class WeeklyGoalTypedReminderTests {
    private static readonly DateTime WeekStart = new(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void TypedUpdate_TargetOnlyPreservesSentDateAndRepeatedUpdatePreservesAuditStamp() {
        var settings = WeeklyGoalReminderSettings.EnabledAt(new TimeOnly(9, 30), TimeSpan.FromMinutes(345));
        var goal = WeeklyGoal.CreateWithReminder(UserId.New(), WeekStart, WeeklyGoalType.DiaryLogging, 5, settings);
        var localDate = new DateOnly(2026, 8, 10);
        goal.MarkReminderSent(localDate, WeekStart.AddHours(4));

        goal.UpdateWithReminder(7, settings, WeekStart.AddHours(5));
        DateTime? changedStamp = goal.ModifiedOnUtc;
        goal.UpdateWithReminder(7, settings, WeekStart.AddHours(6));

        Assert.Multiple(() => {
            Assert.Equal(7, goal.TargetDays);
            Assert.Equal(570, goal.ReminderTimeMinutes);
            Assert.Equal(345, goal.TimeZoneOffsetMinutes);
            Assert.Equal(localDate, goal.LastReminderLocalDate);
            Assert.Equal(WeekStart.AddHours(5), changedStamp);
            Assert.Equal(changedStamp, goal.ModifiedOnUtc);
        });
    }

    [Fact]
    public void TypedUpdate_ChangedConfigurationResetsSentDateAndDisabledClearsSettings() {
        WeeklyGoal goal = CreateEnabledGoal();
        goal.MarkReminderSent(new DateOnly(2026, 8, 10), WeekStart.AddHours(10));

        goal.UpdateWithReminder(5, WeeklyGoalReminderSettings.EnabledAt(new TimeOnly(11, 0), TimeSpan.Zero), WeekStart.AddHours(11));
        Assert.Null(goal.LastReminderLocalDate);
        goal.MarkReminderSent(new DateOnly(2026, 8, 10), WeekStart.AddHours(11));
        goal.UpdateWithReminder(5, WeeklyGoalReminderSettings.Disabled, WeekStart.AddHours(12));

        Assert.Multiple(() => {
            Assert.False(goal.ReminderEnabled);
            Assert.Null(goal.ReminderTimeMinutes);
            Assert.Null(goal.TimeZoneOffsetMinutes);
            Assert.Null(goal.LastReminderLocalDate);
        });
    }

    [Fact]
    public void TypedUpdate_InvalidTimestampDoesNotPartiallyMutateTheGoal() {
        WeeklyGoal goal = CreateEnabledGoal();
        goal.MarkReminderSent(new DateOnly(2026, 8, 10), WeekStart.AddHours(10));
        DateTime? originalStamp = goal.ModifiedOnUtc;

        Assert.Throws<ArgumentOutOfRangeException>(() => goal.UpdateWithReminder(
            7, WeeklyGoalReminderSettings.Disabled, DateTime.SpecifyKind(WeekStart, DateTimeKind.Unspecified)));

        Assert.Multiple(() => {
            Assert.Equal(5, goal.TargetDays);
            Assert.True(goal.ReminderEnabled);
            Assert.Equal(570, goal.ReminderTimeMinutes);
            Assert.Equal(0, goal.TimeZoneOffsetMinutes);
            Assert.Equal(new DateOnly(2026, 8, 10), goal.LastReminderLocalDate);
            Assert.Equal(originalStamp, goal.ModifiedOnUtc);
        });
    }

    [Fact]
    public void TypedPaths_RejectMissingSettings() {
        Assert.Throws<ArgumentNullException>(() => WeeklyGoal.CreateWithReminder(
            UserId.New(), WeekStart, WeeklyGoalType.DiaryLogging, 5, null!));
        WeeklyGoal goal = CreateEnabledGoal();
        Assert.Throws<ArgumentNullException>(() => goal.UpdateWithReminder(5, null!, WeekStart));
    }

    [Fact]
    public void CompatibilityPaths_PreserveValidationOrder() {
        ArgumentException userError = Assert.Throws<ArgumentException>(() => WeeklyGoal.Create(
            UserId.Empty, WeekStart.AddDays(1), WeeklyGoalType.DiaryLogging, 4, reminderEnabled: true, reminderTimeMinutes: null, timeZoneOffsetMinutes: null));
        Assert.Equal("userId", userError.ParamName);
        ArgumentOutOfRangeException weekError = Assert.Throws<ArgumentOutOfRangeException>(() => WeeklyGoal.Create(
            UserId.New(), WeekStart.AddDays(1), WeeklyGoalType.DiaryLogging, 4, reminderEnabled: true, reminderTimeMinutes: null, timeZoneOffsetMinutes: null));
        Assert.Equal("value", weekError.ParamName);
        ArgumentOutOfRangeException typeError = Assert.Throws<ArgumentOutOfRangeException>(() => WeeklyGoal.Create(
            UserId.New(), WeekStart, (WeeklyGoalType)99, 4, reminderEnabled: true, reminderTimeMinutes: null, timeZoneOffsetMinutes: null));
        Assert.Equal("type", typeError.ParamName);
        ArgumentOutOfRangeException targetError = Assert.Throws<ArgumentOutOfRangeException>(() => WeeklyGoal.Create(
            UserId.New(), WeekStart, WeeklyGoalType.DiaryLogging, 4, reminderEnabled: true, reminderTimeMinutes: null, timeZoneOffsetMinutes: null));
        Assert.Equal("targetDays", targetError.ParamName);
        WeeklyGoal goal = CreateEnabledGoal();
        ArgumentOutOfRangeException updateError = Assert.Throws<ArgumentOutOfRangeException>(() =>
            goal.Update(4, reminderEnabled: true, reminderTimeMinutes: null, timeZoneOffsetMinutes: null, DateTime.SpecifyKind(WeekStart, DateTimeKind.Unspecified)));
        Assert.Equal("targetDays", updateError.ParamName);
    }

    private static WeeklyGoal CreateEnabledGoal() => WeeklyGoal.CreateWithReminder(
        UserId.New(), WeekStart, WeeklyGoalType.DiaryLogging, 5,
        WeeklyGoalReminderSettings.EnabledAt(new TimeOnly(9, 30), TimeSpan.Zero));
}
