using FoodDiary.Domain.Entities.WeeklyGoals;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class WeeklyGoalInvariantTests {
    private static readonly DateTime WeekStart = new(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(7)]
    public void Create_AcceptsSupportedTargets(int targetDays) {
        var goal = WeeklyGoal.Create(
            UserId.New(),
            WeekStart,
            WeeklyGoalType.DiaryLogging,
            targetDays,
            reminderEnabled: false,
            reminderTimeMinutes: null,
            timeZoneOffsetMinutes: null);

        Assert.Equal(targetDays, goal.TargetDays);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(8)]
    public void Create_RejectsUnsupportedTargets(int targetDays) {
        Assert.Throws<ArgumentOutOfRangeException>(() => WeeklyGoal.Create(
            UserId.New(),
            WeekStart,
            WeeklyGoalType.DiaryLogging,
            targetDays,
            reminderEnabled: false,
            reminderTimeMinutes: null,
            timeZoneOffsetMinutes: null));
    }

    [Fact]
    public void Create_WithReminder_RequiresTimeAndOffset() {
        Assert.Multiple(
            () => Assert.Throws<ArgumentOutOfRangeException>(() => WeeklyGoal.Create(
                UserId.New(), WeekStart, WeeklyGoalType.DiaryLogging, targetDays: 5,
                reminderEnabled: true, reminderTimeMinutes: null, timeZoneOffsetMinutes: 0)),
            () => Assert.Throws<ArgumentOutOfRangeException>(() => WeeklyGoal.Create(
                UserId.New(), WeekStart, WeeklyGoalType.DiaryLogging, targetDays: 5,
                reminderEnabled: true, reminderTimeMinutes: 21 * 60, timeZoneOffsetMinutes: null)));
    }

    [Fact]
    public void Update_ChangesConfigurationAndResetsReminderDate() {
        var goal = WeeklyGoal.Create(
            UserId.New(), WeekStart, WeeklyGoalType.DiaryLogging, targetDays: 5,
            reminderEnabled: true, reminderTimeMinutes: 21 * 60, timeZoneOffsetMinutes: 240);
        goal.MarkReminderSent(new DateOnly(year: 2026, month: 8, day: 10), WeekStart.AddHours(value: 17));

        goal.Update(targetDays: 3, reminderEnabled: false, reminderTimeMinutes: null,
            timeZoneOffsetMinutes: null, modifiedAtUtc: WeekStart.AddDays(value: 1));

        Assert.Multiple(
            () => Assert.Equal(3, goal.TargetDays),
            () => Assert.False(goal.ReminderEnabled),
            () => Assert.Null(goal.ReminderTimeMinutes),
            () => Assert.Null(goal.TimeZoneOffsetMinutes),
            () => Assert.Null(goal.LastReminderLocalDate));
    }
}
