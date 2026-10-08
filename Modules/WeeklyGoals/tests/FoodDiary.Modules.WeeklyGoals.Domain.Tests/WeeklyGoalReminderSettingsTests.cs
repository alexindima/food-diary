using FoodDiary.Modules.WeeklyGoals.Domain.ValueObjects;

namespace FoodDiary.Modules.WeeklyGoals.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class WeeklyGoalReminderSettingsTests {
    [Theory]
    [InlineData(null, null)]
    [InlineData(-1, 841)]
    [InlineData(int.MaxValue, int.MinValue)]
    public void Disabled_IgnoresAndClearsSuppliedValues(int? timeMinutes, int? offsetMinutes) {
        var reminder = WeeklyGoalReminderSettings.FromMinutes(enabled: false, timeMinutes, offsetMinutes);

        Assert.Same(WeeklyGoalReminderSettings.Disabled, reminder);
        Assert.Multiple(() => {
            Assert.False(reminder.IsEnabled);
            Assert.Null(reminder.LocalTime);
            Assert.Null(reminder.UtcOffset);
            Assert.Null(reminder.LocalTimeMinutes);
            Assert.Null(reminder.UtcOffsetMinutes);
        });
    }

    [Theory]
    [InlineData(0, -840)]
    [InlineData(1439, 840)]
    [InlineData(570, 345)]
    [InlineData(120, -210)]
    public void Enabled_PreservesMinutePrecisionAndDistinctTimeMeanings(int timeMinutes, int offsetMinutes) {
        var reminder = WeeklyGoalReminderSettings.FromMinutes(enabled: true, timeMinutes, offsetMinutes);

        Assert.Multiple(() => {
            Assert.True(reminder.IsEnabled);
            Assert.Equal(TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(timeMinutes)), reminder.LocalTime);
            Assert.Equal(TimeSpan.FromMinutes(offsetMinutes), reminder.UtcOffset);
            Assert.Equal(timeMinutes, reminder.LocalTimeMinutes);
            Assert.Equal(offsetMinutes, reminder.UtcOffsetMinutes);
        });
    }

    [Theory]
    [InlineData(null, null, "timeMinutes")]
    [InlineData(-1, 841, "timeMinutes")]
    [InlineData(1440, 0, "timeMinutes")]
    [InlineData(0, null, "offsetMinutes")]
    [InlineData(0, -841, "offsetMinutes")]
    [InlineData(0, 841, "offsetMinutes")]
    public void Enabled_RejectsMissingAndOutOfRangeInputsInLegacyOrder(int? timeMinutes, int? offsetMinutes, string parameterName) {
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            WeeklyGoalReminderSettings.FromMinutes(enabled: true, timeMinutes, offsetMinutes));

        Assert.Equal(parameterName, error.ParamName);
    }

    [Fact]
    public void TypedSettings_NormalizeClockSecondsAndHaveValueEquality() {
        var precise = WeeklyGoalReminderSettings.EnabledAt(new TimeOnly(9, 30, 59, 999), TimeSpan.FromMinutes(345));
        var wholeMinute = WeeklyGoalReminderSettings.EnabledAt(new TimeOnly(9, 30), TimeSpan.FromMinutes(345));

        Assert.Equal(wholeMinute, precise);
        Assert.NotEqual(WeeklyGoalReminderSettings.Disabled, precise);
    }

    [Theory]
    [InlineData(30)]
    [InlineData(-30)]
    [InlineData(50401)]
    [InlineData(-50401)]
    [InlineData(50460)]
    [InlineData(-50460)]
    public void TypedSettings_RejectFractionalMinutesAndExcessiveOffsets(int offsetSeconds) {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            WeeklyGoalReminderSettings.EnabledAt(TimeOnly.MinValue, TimeSpan.FromSeconds(offsetSeconds)));
    }
}
