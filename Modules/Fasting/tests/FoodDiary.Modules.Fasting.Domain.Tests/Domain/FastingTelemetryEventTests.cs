using FoodDiary.Domain.Entities.Tracking.Fasting;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class FastingTelemetryEventTests {
    [Fact]
    public void Create_WithValidValues_NormalizesAndStoresAllFields() {
        var localTime = new DateTime(2026, 4, 6, 12, 0, 0, DateTimeKind.Local);

        var entity = FastingTelemetryEvent.Create(
            name: "  fasting.completed  ",
            occurredAtUtc: localTime,
            sessionId: " session-1 ",
            protocol: " 16:8 ",
            planType: " intermittent ",
            status: " completed ",
            occurrenceKind: " planned ",
            reminderPresetId: " default ",
            reminderSource: " user ",
            firstReminderHours: 12,
            followUpReminderHours: 14,
            plannedDurationHours: 16,
            actualDurationHours: 16.5,
            hungerLevel: 2,
            energyLevel: 4,
            moodLevel: 5,
            symptomsCount: 1,
            hadNotes: true);

        Assert.NotEqual(Guid.Empty, entity.Id.Value);
        Assert.Equal(localTime.ToUniversalTime(), entity.OccurredAtUtc);
        Assert.Equal("fasting.completed", entity.Name);
        Assert.Equal("session-1", entity.SessionId);
        Assert.Equal("16:8", entity.Protocol);
        Assert.Equal("intermittent", entity.PlanType);
        Assert.Equal("completed", entity.Status);
        Assert.Equal("planned", entity.OccurrenceKind);
        Assert.Equal("default", entity.ReminderPresetId);
        Assert.Equal("user", entity.ReminderSource);
        Assert.Equal(12, entity.FirstReminderHours);
        Assert.Equal(14, entity.FollowUpReminderHours);
        Assert.Equal(16, entity.PlannedDurationHours);
        Assert.Equal(16.5, entity.ActualDurationHours);
        Assert.Equal(2, entity.HungerLevel);
        Assert.Equal(4, entity.EnergyLevel);
        Assert.Equal(5, entity.MoodLevel);
        Assert.Equal(1, entity.SymptomsCount);
        Assert.True(entity.HadNotes);
    }

    [Fact]
    public void Create_WithBlankOptionalValues_StoresNulls() {
        var entity = FastingTelemetryEvent.Create(
            name: "fasting.started",
            occurredAtUtc: DateTime.UtcNow,
            sessionId: " ",
            protocol: "",
            planType: null);

        Assert.Null(entity.SessionId);
        Assert.Null(entity.Protocol);
        Assert.Null(entity.PlanType);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithMissingName_Throws(string name) {
        Assert.Throws<ArgumentException>(() => FastingTelemetryEvent.Create(name, DateTime.UtcNow));
    }

    [Fact]
    public void Create_WithTooLongName_Throws() {
        string name = new('a', 65);

        Assert.Throws<ArgumentOutOfRangeException>(() => FastingTelemetryEvent.Create(name, DateTime.UtcNow));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(169)]
    public void Create_WithInvalidReminderHours_Throws(int hours) {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FastingTelemetryEvent.Create("fasting.reminder", DateTime.UtcNow, firstReminderHours: hours));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Create_WithInvalidScale_Throws(int level) {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FastingTelemetryEvent.Create("fasting.checkin", DateTime.UtcNow, hungerLevel: level));
    }

    [Fact]
    public void Create_WithNegativeSymptomsCount_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FastingTelemetryEvent.Create("fasting.checkin", DateTime.UtcNow, symptomsCount: -1));
    }

    [Fact]
    public void Create_WithUnspecifiedTimestamp_Throws() {
        var occurredAt = new DateTime(2026, 4, 6, 12, 0, 0, DateTimeKind.Unspecified);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FastingTelemetryEvent.Create("fasting.started", occurredAt));
    }
}
