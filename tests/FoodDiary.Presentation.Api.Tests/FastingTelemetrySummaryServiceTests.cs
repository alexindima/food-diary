using System.Text.Json;
using FoodDiary.Application.Fasting.Common;
using FoodDiary.Presentation.Api.Features.Logs.Requests;
using FoodDiary.Presentation.Api.Services;

namespace FoodDiary.Presentation.Api.Tests;

public sealed class FastingTelemetrySummaryServiceTests {
    [Fact]
    public async Task GetSummaryAsync_AggregatesTrackedFastingEvents() {
        var repository = new InMemoryFastingTelemetryEventRepository();
        var service = new FastingTelemetrySummaryService(repository);
        var timestamp = DateTime.UtcNow.AddHours(-1).ToString("O");

        await service.RecordAsync(CreateRequest("fasting.reminder-preset.selected", timestamp, """
            {"reminderPresetId":"steady","firstReminderHours":16,"followUpReminderHours":24}
            """), CancellationToken.None);
        await service.RecordAsync(CreateRequest("fasting.reminder-timing.saved", timestamp, """
            {"source":"preset","reminderPresetId":"steady","firstReminderHours":16,"followUpReminderHours":24}
            """), CancellationToken.None);
        await service.RecordAsync(CreateRequest("fasting.session.started", timestamp, """
            {"sessionId":"s1","plannedDurationHours":16,"reminderPresetId":"steady","firstReminderHours":16,"followUpReminderHours":24}
            """), CancellationToken.None);
        await service.RecordAsync(CreateRequest("fasting.check-in.saved", timestamp, """
            {"sessionId":"s1","hungerLevel":3,"reminderPresetId":"steady","firstReminderHours":16,"followUpReminderHours":24}
            """), CancellationToken.None);
        await service.RecordAsync(CreateRequest("fasting.session.completed", timestamp, """
            {"sessionId":"s1","actualDurationHours":15.5,"reminderPresetId":"steady","firstReminderHours":16,"followUpReminderHours":24}
            """), CancellationToken.None);

        var summary = await service.GetSummaryAsync(24, CancellationToken.None);

        Assert.Equal(1, summary.StartedSessions);
        Assert.Equal(1, summary.CompletedSessions);
        Assert.Equal(1, summary.SavedCheckIns);
        Assert.Equal(1, summary.ReminderPresetSelections);
        Assert.Equal(1, summary.ReminderTimingSaves);
        Assert.Equal(1, summary.PresetReminderTimingSaves);
        Assert.Equal(0, summary.ManualReminderTimingSaves);
        Assert.Equal(100, summary.CompletionRatePercent);
        Assert.Equal(100, summary.CheckInRatePercent);
        Assert.Equal(15.5, summary.AverageCompletedDurationHours);
        Assert.Single(summary.TopPresets);
        Assert.Equal("steady", summary.TopPresets[0].PresetId);
        Assert.Equal(1, summary.TopPresets[0].StartedSessions);
        Assert.Equal(1, summary.TopPresets[0].CompletedSessions);
        Assert.Equal(1, summary.TopPresets[0].SavedCheckIns);
    }

    [Fact]
    public async Task RecordAsync_IgnoresNonFastingTelemetry() {
        var repository = new InMemoryFastingTelemetryEventRepository();
        var service = new FastingTelemetrySummaryService(repository);

        await service.RecordAsync(CreateRequest("notifications.preference.changed", DateTime.UtcNow.ToString("O")), CancellationToken.None);

        var summary = await service.GetSummaryAsync(24, CancellationToken.None);

        Assert.Equal(0, summary.StartedSessions);
        Assert.Equal(0, summary.ReminderPresetSelections);
        Assert.Empty(summary.TopPresets);
    }

    private static ClientTelemetryLogHttpRequest CreateRequest(string name, string timestamp, string? detailsJson = null) {
        JsonElement? details = null;
        if (detailsJson is not null) {
            details = JsonSerializer.Deserialize<JsonElement>(detailsJson);
        }

        return new ClientTelemetryLogHttpRequest(
            Category: "user_action",
            Name: name,
            Level: "info",
            Timestamp: timestamp,
            Details: details);
    }

    private sealed class InMemoryFastingTelemetryEventRepository : IFastingTelemetryEventRepository {
        private readonly List<FastingTelemetryEventRecord> _events = [];

        public Task AddAsync(FastingTelemetryEventRecord record, CancellationToken cancellationToken = default) {
            _events.Add(record);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<FastingTelemetryEventRecord>> GetSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken = default) {
            return Task.FromResult<IReadOnlyList<FastingTelemetryEventRecord>>(_events.Where(x => x.OccurredAtUtc >= sinceUtc).ToList());
        }
    }
}
