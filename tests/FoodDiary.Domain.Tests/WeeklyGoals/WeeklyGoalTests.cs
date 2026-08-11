using FoodDiary.Domain.Entities.WeeklyGoals;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests.WeeklyGoals;

[ExcludeFromCodeCoverage]
public sealed class WeeklyGoalTests {
    private static readonly DateTime Monday = new(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void MarkReminderSent_WhenReminderDisabled_Throws() {
        WeeklyGoal goal = Create(reminderEnabled: false);

        Assert.Throws<InvalidOperationException>(() => goal.MarkReminderSent(new DateOnly(2026, 8, 10), Monday));
    }

    [Fact]
    public void Create_WithEmptyUserId_Throws() => Assert.Throws<ArgumentException>(() => Create(userId: UserId.Empty));

    [Fact]
    public void Create_WithNonMondayWeekStart_Throws() => Assert.Throws<ArgumentOutOfRangeException>(() => Create(weekStart: Monday.AddDays(1)));

    [Fact]
    public void Create_WithUnsupportedType_Throws() => Assert.Throws<ArgumentOutOfRangeException>(() => Create(type: (WeeklyGoalType)int.MaxValue));

    private static WeeklyGoal Create(
        UserId? userId = null,
        DateTime? weekStart = null,
        WeeklyGoalType type = WeeklyGoalType.DiaryLogging,
        bool reminderEnabled = true) => WeeklyGoal.Create(
            userId ?? UserId.New(), weekStart ?? Monday, type, 5, reminderEnabled,
            reminderEnabled ? 540 : null, reminderEnabled ? 0 : null);
}
