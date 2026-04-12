using System.Globalization;
using System.Text.Json;
using FoodDiary.Application.Fasting.Common;
using FoodDiary.Presentation.Api.Features.Logs.Requests;

namespace FoodDiary.Presentation.Api.Services;

public interface IFastingTelemetrySummaryService {
    Task RecordAsync(ClientTelemetryLogHttpRequest request, CancellationToken cancellationToken);
    Task<FastingTelemetrySummarySnapshot> GetSummaryAsync(int windowHours, CancellationToken cancellationToken);
}

public sealed class FastingTelemetrySummaryService(IFastingTelemetryEventRepository repository) : IFastingTelemetrySummaryService {
    public async Task RecordAsync(ClientTelemetryLogHttpRequest request, CancellationToken cancellationToken) {
        if (!string.Equals(request.Category, "user_action", StringComparison.OrdinalIgnoreCase) ||
            !request.Name.StartsWith("fasting.", StringComparison.OrdinalIgnoreCase)) {
            return;
        }

        var details = ReadDetails(request.Details);
        var record = new FastingTelemetryEventRecord(
            request.Name,
            ParseTimestampUtc(request.Timestamp),
            ReadString(details, "sessionId"),
            ReadString(details, "protocol"),
            ReadString(details, "planType"),
            ReadString(details, "status"),
            ReadString(details, "occurrenceKind"),
            ReadString(details, "reminderPresetId") ?? ReadString(details, "presetId"),
            ReadString(details, "source"),
            ReadInt(details, "firstReminderHours"),
            ReadInt(details, "followUpReminderHours"),
            ReadInt(details, "plannedDurationHours"),
            ReadDouble(details, "actualDurationHours"),
            ReadInt(details, "hungerLevel"),
            ReadInt(details, "energyLevel"),
            ReadInt(details, "moodLevel"),
            ReadInt(details, "symptomsCount"),
            ReadBool(details, "hadNotes"));

        await repository.AddAsync(record, cancellationToken);
    }

    public async Task<FastingTelemetrySummarySnapshot> GetSummaryAsync(int windowHours, CancellationToken cancellationToken) {
        var normalizedWindowHours = Math.Clamp(windowHours, 1, 168);
        var windowStartUtc = DateTime.UtcNow.AddHours(-normalizedWindowHours);
        var events = await repository.GetSinceAsync(windowStartUtc, cancellationToken);

        var startedEvents = events.Where(x => x.Name == "fasting.session.started").ToArray();
        var completedEvents = events.Where(x => x.Name == "fasting.session.completed").ToArray();
        var checkInEvents = events.Where(x => x.Name == "fasting.check-in.saved").ToArray();
        var reminderPresetSelections = events.Where(x => x.Name == "fasting.reminder-preset.selected").ToArray();
        var reminderTimingSaves = events.Where(x => x.Name == "fasting.reminder-timing.saved").ToArray();
        var completedDurations = completedEvents
            .Where(x => x.ActualDurationHours.HasValue)
            .Select(x => x.ActualDurationHours!.Value)
            .ToArray();

        var topPresets = startedEvents
            .GroupBy(x => x.ReminderPresetId ?? "custom", StringComparer.OrdinalIgnoreCase)
            .Select(group => {
                var presetId = group.Key;
                var startedSessions = group.Count();
                var completedSessions = completedEvents.Count(x => string.Equals(x.ReminderPresetId ?? "custom", presetId, StringComparison.OrdinalIgnoreCase));
                var savedCheckIns = checkInEvents.Count(x => string.Equals(x.ReminderPresetId ?? "custom", presetId, StringComparison.OrdinalIgnoreCase));
                var selectionCount = reminderPresetSelections.Count(x => string.Equals(x.ReminderPresetId ?? "custom", presetId, StringComparison.OrdinalIgnoreCase));
                var timingSaveCount = reminderTimingSaves.Count(x => string.Equals(x.ReminderPresetId ?? "custom", presetId, StringComparison.OrdinalIgnoreCase));
                var latestEvent = group.OrderByDescending(x => x.OccurredAtUtc).First();

                return new FastingTelemetryPresetSnapshot(
                    presetId,
                    selectionCount,
                    timingSaveCount,
                    latestEvent.FirstReminderHours,
                    latestEvent.FollowUpReminderHours,
                    startedSessions,
                    completedSessions,
                    savedCheckIns,
                    startedSessions > 0 ? Math.Round((double)completedSessions / startedSessions * 100, 1) : 0,
                    startedSessions > 0 ? Math.Round((double)savedCheckIns / startedSessions * 100, 1) : 0);
            })
            .OrderByDescending(x => x.StartedSessions)
            .ThenByDescending(x => x.SelectionCount)
            .ThenBy(x => x.PresetId, StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToArray();

        return new FastingTelemetrySummarySnapshot(
            normalizedWindowHours,
            DateTime.UtcNow,
            startedEvents.Length,
            completedEvents.Length,
            checkInEvents.Length,
            reminderPresetSelections.Length,
            reminderTimingSaves.Length,
            reminderTimingSaves.Count(x => string.Equals(x.ReminderSource, "preset", StringComparison.OrdinalIgnoreCase)),
            reminderTimingSaves.Count(x => string.Equals(x.ReminderSource, "manual", StringComparison.OrdinalIgnoreCase)),
            startedEvents.Length > 0 ? Math.Round((double)completedEvents.Length / startedEvents.Length * 100, 1) : 0,
            startedEvents.Length > 0 ? Math.Round((double)checkInEvents.Length / startedEvents.Length * 100, 1) : 0,
            completedDurations.Length > 0 ? Math.Round(completedDurations.Average(), 1) : null,
            checkInEvents.MaxBy(x => x.OccurredAtUtc)?.OccurredAtUtc,
            events.MaxBy(x => x.OccurredAtUtc)?.OccurredAtUtc,
            topPresets);
    }

    private static DateTime ParseTimestampUtc(string? timestamp) {
        if (DateTime.TryParse(
                timestamp,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed)) {
            return parsed;
        }

        return DateTime.UtcNow;
    }

    private static IReadOnlyDictionary<string, string> ReadDetails(JsonElement? details) {
        if (details is null || details.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined) {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        if (details.Value.ValueKind != JsonValueKind.Object) {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in details.Value.EnumerateObject()) {
            var value = property.Value.ValueKind switch {
                JsonValueKind.String => property.Value.GetString(),
                JsonValueKind.Number => property.Value.GetRawText(),
                JsonValueKind.True => bool.TrueString,
                JsonValueKind.False => bool.FalseString,
                _ => null,
            };

            if (!string.IsNullOrWhiteSpace(value)) {
                result[property.Name] = value;
            }
        }

        return result;
    }

    private static string? ReadString(IReadOnlyDictionary<string, string> details, string key) =>
        details.TryGetValue(key, out var value) ? value : null;

    private static int? ReadInt(IReadOnlyDictionary<string, string> details, string key) =>
        details.TryGetValue(key, out var value) && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;

    private static double? ReadDouble(IReadOnlyDictionary<string, string> details, string key) =>
        details.TryGetValue(key, out var value) && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;

    private static bool? ReadBool(IReadOnlyDictionary<string, string> details, string key) =>
        details.TryGetValue(key, out var value) && bool.TryParse(value, out var parsed)
            ? parsed
            : null;
}

public sealed record FastingTelemetrySummarySnapshot(
    int WindowHours,
    DateTime GeneratedAtUtc,
    int StartedSessions,
    int CompletedSessions,
    int SavedCheckIns,
    int ReminderPresetSelections,
    int ReminderTimingSaves,
    int PresetReminderTimingSaves,
    int ManualReminderTimingSaves,
    double CompletionRatePercent,
    double CheckInRatePercent,
    double? AverageCompletedDurationHours,
    DateTime? LastCheckInAtUtc,
    DateTime? LastEventAtUtc,
    IReadOnlyList<FastingTelemetryPresetSnapshot> TopPresets);

public sealed record FastingTelemetryPresetSnapshot(
    string PresetId,
    int SelectionCount,
    int TimingSaveCount,
    int? FirstReminderHours,
    int? FollowUpReminderHours,
    int StartedSessions,
    int CompletedSessions,
    int SavedCheckIns,
    double CompletionRatePercent,
    double CheckInRatePercent);
