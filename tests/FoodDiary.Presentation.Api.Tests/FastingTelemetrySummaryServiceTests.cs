using System.Text.Json;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Fasting.Commands.RecordFastingTelemetry;
using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Application.Fasting.Queries.GetFastingTelemetrySummary;
using FoodDiary.Application.Fasting.Services;
using FoodDiary.Presentation.Api.Features.Logs.Requests;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class FastingTelemetrySummaryServiceTests {
    [Fact]
    public async Task GetSummaryAsync_AggregatesTrackedFastingEvents() {
        var repository = new InMemoryFastingTelemetryEventRepository();
        GetFastingTelemetrySummaryQueryHandler handler = CreateHandler(repository);
        string timestamp = DateTime.UtcNow.AddHours(-1).ToString("O");

        await RecordAsync(repository, CreateRequest("fasting.reminder-preset.selected", timestamp, """
            {"reminderPresetId":"steady","firstReminderHours":16,"followUpReminderHours":24}
            """), CancellationToken.None);
        await RecordAsync(repository, CreateRequest("fasting.reminder-timing.saved", timestamp, """
            {"source":"preset","reminderPresetId":"steady","firstReminderHours":16,"followUpReminderHours":24}
            """), CancellationToken.None);
        await RecordAsync(repository, CreateRequest("fasting.session.started", timestamp, """
            {"sessionId":"s1","plannedDurationHours":16,"reminderPresetId":"steady","firstReminderHours":16,"followUpReminderHours":24}
            """), CancellationToken.None);
        await RecordAsync(repository, CreateRequest("fasting.check-in.saved", timestamp, """
            {"sessionId":"s1","hungerLevel":3,"reminderPresetId":"steady","firstReminderHours":16,"followUpReminderHours":24}
            """), CancellationToken.None);
        await RecordAsync(repository, CreateRequest("fasting.session.completed", timestamp, """
            {"sessionId":"s1","actualDurationHours":15.5,"reminderPresetId":"steady","firstReminderHours":16,"followUpReminderHours":24}
            """), CancellationToken.None);

        Result<FastingTelemetrySummaryModel> result = await handler.Handle(new GetFastingTelemetrySummaryQuery(24), CancellationToken.None);
        FastingTelemetrySummaryModel summary = result.Value;

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

        await RecordAsync(repository, CreateRequest("notifications.preference.changed", DateTime.UtcNow.ToString("O")), CancellationToken.None);

        GetFastingTelemetrySummaryQueryHandler handler = CreateHandler(repository);
        Result<FastingTelemetrySummaryModel> result = await handler.Handle(new GetFastingTelemetrySummaryQuery(24), CancellationToken.None);
        FastingTelemetrySummaryModel summary = result.Value;

        Assert.Equal(0, summary.StartedSessions);
        Assert.Equal(0, summary.ReminderPresetSelections);
        Assert.Empty(summary.TopPresets);
    }

    [Fact]
    public async Task RecordAsync_WithInvalidTimestamp_UsesCurrentUtcTimestamp() {
        var repository = new InMemoryFastingTelemetryEventRepository();
        DateTime fallbackNow = new(2026, 6, 30, 10, 15, 0, DateTimeKind.Utc);

        await RecordAsync(repository, CreateRequest("fasting.session.started", "not-a-date", "{}"), CancellationToken.None, new FixedTimeProvider(fallbackNow));

        FastingTelemetryEventRecord record = Assert.Single(repository.Events);
        Assert.Equal(fallbackNow, record.OccurredAtUtc);
    }

    [Fact]
    public async Task RecordAsync_WithNullDetails_RecordsEventWithoutDetailValues() {
        var repository = new InMemoryFastingTelemetryEventRepository();

        await RecordAsync(repository, CreateRequest("fasting.session.started", DateTime.UtcNow.ToString("O")), CancellationToken.None);

        FastingTelemetryEventRecord record = Assert.Single(repository.Events);
        Assert.Null(record.SessionId);
        Assert.Null(record.Protocol);
        Assert.Null(record.PlannedDurationHours);
    }

    [Fact]
    public async Task RecordAsync_WithUndefinedDetails_RecordsEventWithoutDetailValues() {
        var repository = new InMemoryFastingTelemetryEventRepository();
        JsonElement? details = default(JsonElement);

        await RecordAsync(repository, CreateRequest("fasting.session.started", DateTime.UtcNow.ToString("O"), details), CancellationToken.None);

        FastingTelemetryEventRecord record = Assert.Single(repository.Events);
        Assert.Null(record.SessionId);
        Assert.Null(record.Protocol);
        Assert.Null(record.PlannedDurationHours);
    }

    [Fact]
    public async Task RecordAsync_WithJsonNullDetails_RecordsEventWithoutDetailValues() {
        var repository = new InMemoryFastingTelemetryEventRepository();
        JsonElement details = JsonSerializer.Deserialize<JsonElement>("null");

        await RecordAsync(repository, CreateRequest("fasting.session.started", DateTime.UtcNow.ToString("O"), details), CancellationToken.None);

        FastingTelemetryEventRecord record = Assert.Single(repository.Events);
        Assert.Null(record.SessionId);
        Assert.Null(record.Protocol);
        Assert.Null(record.PlannedDurationHours);
    }

    [Fact]
    public async Task RecordAsync_WithNonObjectDetails_RecordsEventWithoutDetailValues() {
        var repository = new InMemoryFastingTelemetryEventRepository();
        JsonElement details = JsonSerializer.Deserialize<JsonElement>("\"not-an-object\"");

        await RecordAsync(repository, CreateRequest("fasting.session.started", DateTime.UtcNow.ToString("O"), details), CancellationToken.None);

        FastingTelemetryEventRecord record = Assert.Single(repository.Events);
        Assert.Null(record.SessionId);
        Assert.Null(record.Protocol);
        Assert.Null(record.PlannedDurationHours);
    }

    [Fact]
    public async Task RecordAsync_WithBooleanAndUnsupportedDetails_ParsesBooleansAndIgnoresUnsupportedValues() {
        var repository = new InMemoryFastingTelemetryEventRepository();

        await RecordAsync(repository, CreateRequest("fasting.check-in.saved", DateTime.UtcNow.ToString("O"), """
            {
                "hadNotes": true,
                "protocol": false,
                "sessionId": {"value":"ignored"},
                "hungerLevel": "",
                "actualDurationHours": "not-a-number"
            }
            """), CancellationToken.None);

        FastingTelemetryEventRecord record = Assert.Single(repository.Events);
        Assert.True(record.HadNotes);
        Assert.Equal(bool.FalseString, record.Protocol);
        Assert.Null(record.SessionId);
        Assert.Null(record.HungerLevel);
        Assert.Null(record.ActualDurationHours);
    }

    [Fact]
    public async Task RecordAsync_WithInvalidNumericAndBooleanDetails_RecordsNullParsedValues() {
        var repository = new InMemoryFastingTelemetryEventRepository();

        await RecordAsync(repository, CreateRequest("fasting.session.completed", DateTime.UtcNow.ToString("O"), """
            {
                "firstReminderHours": "bad",
                "actualDurationHours": "bad",
                "hadNotes": "bad"
            }
            """), CancellationToken.None);

        FastingTelemetryEventRecord record = Assert.Single(repository.Events);
        Assert.Null(record.FirstReminderHours);
        Assert.Null(record.ActualDurationHours);
        Assert.Null(record.HadNotes);
    }

    private static ClientTelemetryLogHttpRequest CreateRequest(string name, string timestamp, string? detailsJson = null) {
        JsonElement? details = null;
        if (detailsJson is not null) {
            details = JsonSerializer.Deserialize<JsonElement>(detailsJson);
        }

        return CreateRequest(name, timestamp, details);
    }

    private static ClientTelemetryLogHttpRequest CreateRequest(string name, string timestamp, JsonElement? details) {
        return new ClientTelemetryLogHttpRequest(
            Category: "user_action",
            Name: name,
            Level: "info",
            Timestamp: timestamp,
            Details: details);
    }

    private static GetFastingTelemetrySummaryQueryHandler CreateHandler(IFastingTelemetryEventRepository repository) =>
        new(new FastingTelemetrySummaryReadService(repository, TimeProvider.System));

    private static async Task RecordAsync(
        IFastingTelemetryEventRepository repository,
        ClientTelemetryLogHttpRequest request,
        CancellationToken cancellationToken,
        TimeProvider? timeProvider = null) {
        var handler = new RecordFastingTelemetryCommandHandler(repository, timeProvider ?? TimeProvider.System);
        await handler.Handle(
            new RecordFastingTelemetryCommand(
                request.Category,
                request.Name,
                request.Timestamp,
                request.Details),
            cancellationToken).ConfigureAwait(false);
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }

    [ExcludeFromCodeCoverage]
    private sealed class InMemoryFastingTelemetryEventRepository : IFastingTelemetryEventRepository {
        private readonly List<FastingTelemetryEventRecord> _events = [];

        public IReadOnlyList<FastingTelemetryEventRecord> Events => _events;

        public Task AddAsync(FastingTelemetryEventRecord record, CancellationToken cancellationToken = default) {
            _events.Add(record);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<FastingTelemetryEventRecord>> GetSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken = default) {
            return Task.FromResult<IReadOnlyList<FastingTelemetryEventRecord>>(_events.Where(x => x.OccurredAtUtc >= sinceUtc).ToList());
        }
    }
}
