using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Users.Domain.Entities;

namespace FoodDiary.Modules.Users.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class FastingReminderSettingsTests {
    [Theory]
    [InlineData(1, 168)]
    [InlineData(12, 20)]
    public void ValidSettings_KeepOrderedElapsedHours(int first, int followUp) {
        var settings = FastingReminderSettings.FromHours(first, followUp);
        var schedule = FastingReminderSchedule.FromSettings(settings);
        Assert.Multiple(
            () => Assert.Equal(first, settings.FirstHours),
            () => Assert.Equal(followUp, settings.FollowUpHours),
            () => Assert.Equal(new[] { first, followUp }, schedule.DueHours),
            () => Assert.False(schedule.IsLegacy));
    }

    [Theory]
    [InlineData(0, 20, "fastingCheckInReminderHours")]
    [InlineData(12, 169, "fastingCheckInFollowUpReminderHours")]
    [InlineData(20, 12, "fastingCheckInFollowUpReminderHours")]
    [InlineData(12, 12, "fastingCheckInFollowUpReminderHours")]
    public void InvalidSettings_RetainParameterContracts(int first, int followUp, string parameterName) {
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => FastingReminderSettings.FromHours(first, followUp));
        Assert.Equal(parameterName, error.ParamName);
    }

    [Fact]
    public void PartialUpdates_MergeAgainstExistingCounterpartBeforeCheckingOrder() {
        var user = User.Create("reminder-partial@example.com", "hash");
        user.UpdatePreferences(new UserPreferenceUpdate(new FastingReminderDelayUpdate(FollowUpHours: 36)));
        user.UpdatePreferences(new UserPreferenceUpdate(new FastingReminderDelayUpdate(FirstHours: 24)));
        Assert.Multiple(
            () => Assert.Equal(24, user.FastingCheckInReminderHours),
            () => Assert.Equal(36, user.FastingCheckInFollowUpReminderHours));
        Assert.Throws<ArgumentOutOfRangeException>(() => user.UpdatePreferences(
            new UserPreferenceUpdate(new FastingReminderDelayUpdate(FollowUpHours: 24))));
    }

    [Fact]
    public void InvalidUpdates_DoNotApplyUnrelatedPreferencesOrTouchAuditState() {
        var user = User.Create("reminder-atomic@example.com", "hash");
        Assert.Throws<ArgumentOutOfRangeException>(() => user.UpdatePreferences(
            new UserPreferenceUpdate(new FastingReminderDelayUpdate(FirstHours: 20), PushNotificationsEnabled: true)));
        Assert.Multiple(
            () => Assert.False(user.PushNotificationsEnabled),
            () => Assert.Null(user.Preferences.ModifiedOnUtc),
            () => Assert.Equal(12, user.FastingCheckInReminderHours),
            () => Assert.Equal(20, user.FastingCheckInFollowUpReminderHours));
    }

    [Fact]
    public void IndividualHoursAndSurfaceStyle_KeepExistingValidationPrecedence() {
        var user = User.Create("reminder-precedence@example.com", "hash");
        ArgumentOutOfRangeException first = Assert.Throws<ArgumentOutOfRangeException>(() => user.UpdatePreferences(
            new UserPreferenceUpdate(new FastingReminderDelayUpdate(FirstHours: 0), SurfaceStyle: "unsupported")));
        ArgumentOutOfRangeException surface = Assert.Throws<ArgumentOutOfRangeException>(() => user.UpdatePreferences(
            new UserPreferenceUpdate(new FastingReminderDelayUpdate(FirstHours: 20), SurfaceStyle: "unsupported")));
        Assert.Equal("fastingCheckInReminderHours", first.ParamName);
        Assert.Equal("surfaceStyle", surface.ParamName);
    }

    [Theory]
    [InlineData(20, 12)]
    [InlineData(12, 12)]
    [InlineData(0, 20)]
    public void StoredLegacySchedules_PreserveOriginalFieldsAndSortedDistinctDueHours(int first, int followUp) {
        var schedule = FastingReminderSchedule.FromStoredHours(first, followUp);
        Assert.Multiple(
            () => Assert.Equal(first, schedule.FirstHours),
            () => Assert.Equal(followUp, schedule.FollowUpHours),
            () => Assert.Equal(new[] { first, followUp }.Distinct().Order(), schedule.DueHours),
            () => Assert.True(schedule.IsLegacy),
            () => Assert.Null(schedule.Settings));
    }

    [Fact]
    public void EquivalentSchedules_HaveValueEqualityForPreferenceNoOps() {
        Assert.Equal(FastingReminderSchedule.Default, FastingReminderSchedule.FromStoredHours(12, 20));
    }
}
