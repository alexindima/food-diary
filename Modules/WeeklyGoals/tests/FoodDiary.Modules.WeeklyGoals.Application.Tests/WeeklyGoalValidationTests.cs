using System.Globalization;
using FluentValidation.Results;
using FoodDiary.Modules.WeeklyGoals.Application.Commands.UpsertWeeklyGoal;

namespace FoodDiary.Modules.WeeklyGoals.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class WeeklyGoalValidationTests {
    [Theory]
    [InlineData("2026-10-11T20:30:00Z", null, "2026-10-12", true)]
    [InlineData("2026-10-11T20:30:00Z", null, "2026-10-19", false)]
    [InlineData("2026-10-11T20:30:00Z", "Asia/Tbilisi", "2026-10-19", true)]
    [InlineData("2026-10-11T20:30:00Z", "Asia/Kathmandu", "2026-10-19", true)]
    [InlineData("2026-10-11T10:30:00Z", "Pacific/Kiritimati", "2026-10-19", true)]
    [InlineData("2026-10-11T22:30:00Z", "Europe/Berlin", "2026-10-19", true)]
    [InlineData("2026-10-12T03:30:00Z", "America/Los_Angeles", "2026-10-12", true)]
    [InlineData("2026-10-12T03:30:00Z", "America/Los_Angeles", "2026-10-19", false)]
    [InlineData("2026-10-12T01:30:00Z", "America/St_Johns", "2026-10-19", false)]
    [InlineData("2026-10-12T03:30:00Z", "UTC", "2026-10-19", true)]
    [InlineData("2026-10-11T20:30:00Z", "Asia/Tbilisi", "2026-10-26", false)]
    [InlineData("2026-10-11T20:30:00Z", "Asia/Tbilisi", "2026-10-05", true)]
    [InlineData("2026-10-11T20:30:00Z", "Asia/Tbilisi", "2026-10-20", false)]
    public async Task AdjacentWeeksFollowTheRequestedCalendarAsync(
        string utcNow, string? timeZoneId, string requestedWeek, bool expectedValid) {
        var clock = new FixedTimeProvider(DateTimeOffset.Parse(utcNow, CultureInfo.InvariantCulture));
        var validator = new UpsertWeeklyGoalCommandValidator(clock);
        var command = new UpsertWeeklyGoalCommand(
            Guid.NewGuid(), DateOnly.Parse(requestedWeek, CultureInfo.InvariantCulture),
            TargetDays: 5, ReminderEnabled: false, ReminderTime: null,
            TimeZoneOffsetMinutes: null, TimeZoneId: timeZoneId);

        ValidationResult result = await validator.ValidateAsync(command);

        Assert.Equal(expectedValid, result.IsValid);
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
