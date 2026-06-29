using FoodDiary.Application.Abstractions.Fasting.Common;

namespace FoodDiary.Presentation.Api.Services;

public sealed class FastingTelemetrySummaryService(IFastingTelemetryEventRepository repository) : IFastingTelemetrySummaryService {
    public async Task<FastingTelemetrySummarySnapshot> GetSummaryAsync(int windowHours, CancellationToken cancellationToken) {
        int normalizedWindowHours = Math.Clamp(windowHours, 1, 168);
        DateTime windowStartUtc = DateTime.UtcNow.AddHours(-normalizedWindowHours);
        IReadOnlyList<FastingTelemetryEventRecord> events = await repository.GetSinceAsync(windowStartUtc, cancellationToken).ConfigureAwait(false);

        FastingTelemetryEventRecord[] startedEvents = [.. events.Where(x => string.Equals(x.Name, "fasting.session.started", StringComparison.Ordinal))];
        FastingTelemetryEventRecord[] completedEvents = [.. events.Where(x => string.Equals(x.Name, "fasting.session.completed", StringComparison.Ordinal))];
        FastingTelemetryEventRecord[] checkInEvents = [.. events.Where(x => string.Equals(x.Name, "fasting.check-in.saved", StringComparison.Ordinal))];
        FastingTelemetryEventRecord[] reminderPresetSelections = [.. events.Where(x => string.Equals(x.Name, "fasting.reminder-preset.selected", StringComparison.Ordinal))];
        FastingTelemetryEventRecord[] reminderTimingSaves = [.. events.Where(x => string.Equals(x.Name, "fasting.reminder-timing.saved", StringComparison.Ordinal))];
        double[] completedDurations = [.. completedEvents
            .Where(x => x.ActualDurationHours.HasValue)
            .Select(x => x.ActualDurationHours!.Value)];

        FastingTelemetryPresetSnapshot[] topPresets = [.. startedEvents
            .GroupBy(x => x.ReminderPresetId ?? "custom", StringComparer.OrdinalIgnoreCase)
            .Select(group => {
                string presetId = group.Key;
                int startedSessions = group.Count();
                int completedSessions = completedEvents.Count(x => string.Equals(x.ReminderPresetId ?? "custom", presetId, StringComparison.OrdinalIgnoreCase));
                int savedCheckIns = checkInEvents.Count(x => string.Equals(x.ReminderPresetId ?? "custom", presetId, StringComparison.OrdinalIgnoreCase));
                int selectionCount = reminderPresetSelections.Count(x => string.Equals(x.ReminderPresetId ?? "custom", presetId, StringComparison.OrdinalIgnoreCase));
                int timingSaveCount = reminderTimingSaves.Count(x => string.Equals(x.ReminderPresetId ?? "custom", presetId, StringComparison.OrdinalIgnoreCase));
                FastingTelemetryEventRecord latestEvent = group.OrderByDescending(x => x.OccurredAtUtc).First();

                return new FastingTelemetryPresetSnapshot(
                    presetId,
                    selectionCount,
                    timingSaveCount,
                    latestEvent.FirstReminderHours,
                    latestEvent.FollowUpReminderHours,
                    startedSessions,
                    completedSessions,
                    savedCheckIns,
                    startedSessions > 0 ? Math.Round((double)completedSessions / startedSessions * 100, 1, MidpointRounding.ToEven) : 0,
                    startedSessions > 0 ? Math.Round((double)savedCheckIns / startedSessions * 100, 1, MidpointRounding.ToEven) : 0);
            })
            .OrderByDescending(x => x.StartedSessions)
            .ThenByDescending(x => x.SelectionCount)
            .ThenBy(x => x.PresetId, StringComparer.OrdinalIgnoreCase)
            .Take(5)];

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
            startedEvents.Length > 0 ? Math.Round((double)completedEvents.Length / startedEvents.Length * 100, 1, MidpointRounding.ToEven) : 0,
            startedEvents.Length > 0 ? Math.Round((double)checkInEvents.Length / startedEvents.Length * 100, 1, MidpointRounding.ToEven) : 0,
            completedDurations.Length > 0 ? Math.Round(completedDurations.Average(), 1, MidpointRounding.ToEven) : null,
            checkInEvents.MaxBy(x => x.OccurredAtUtc)?.OccurredAtUtc,
            events.MaxBy(x => x.OccurredAtUtc)?.OccurredAtUtc,
            topPresets);
    }

}
