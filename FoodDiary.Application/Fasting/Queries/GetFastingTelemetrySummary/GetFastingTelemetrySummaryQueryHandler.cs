using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Fasting.Models;

namespace FoodDiary.Application.Fasting.Queries.GetFastingTelemetrySummary;

public sealed class GetFastingTelemetrySummaryQueryHandler(
    IFastingTelemetryEventRepository repository,
    TimeProvider timeProvider)
    : IQueryHandler<GetFastingTelemetrySummaryQuery, Result<FastingTelemetrySummaryModel>> {
    public async Task<Result<FastingTelemetrySummaryModel>> Handle(
        GetFastingTelemetrySummaryQuery query,
        CancellationToken cancellationToken) {
        int normalizedWindowHours = Math.Clamp(query.Hours, 1, 168);
        DateTime nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        DateTime windowStartUtc = nowUtc.AddHours(-normalizedWindowHours);
        IReadOnlyList<FastingTelemetryEventRecord> events = await repository.GetSinceAsync(windowStartUtc, cancellationToken).ConfigureAwait(false);

        FastingTelemetryEventRecord[] startedEvents = [.. events.Where(static x => string.Equals(x.Name, "fasting.session.started", StringComparison.Ordinal))];
        FastingTelemetryEventRecord[] completedEvents = [.. events.Where(static x => string.Equals(x.Name, "fasting.session.completed", StringComparison.Ordinal))];
        FastingTelemetryEventRecord[] checkInEvents = [.. events.Where(static x => string.Equals(x.Name, "fasting.check-in.saved", StringComparison.Ordinal))];
        FastingTelemetryEventRecord[] reminderPresetSelections = [.. events.Where(static x => string.Equals(x.Name, "fasting.reminder-preset.selected", StringComparison.Ordinal))];
        FastingTelemetryEventRecord[] reminderTimingSaves = [.. events.Where(static x => string.Equals(x.Name, "fasting.reminder-timing.saved", StringComparison.Ordinal))];
        double[] completedDurations = [.. completedEvents
            .Where(static x => x.ActualDurationHours.HasValue)
            .Select(static x => x.ActualDurationHours!.Value)];

        FastingTelemetryPresetSummaryModel[] topPresets = [.. startedEvents
            .GroupBy(static x => x.ReminderPresetId ?? "custom", StringComparer.OrdinalIgnoreCase)
            .Select(group => {
                string presetId = group.Key;
                int startedSessions = group.Count();
                int completedSessions = completedEvents.Count(x => string.Equals(x.ReminderPresetId ?? "custom", presetId, StringComparison.OrdinalIgnoreCase));
                int savedCheckIns = checkInEvents.Count(x => string.Equals(x.ReminderPresetId ?? "custom", presetId, StringComparison.OrdinalIgnoreCase));
                int selectionCount = reminderPresetSelections.Count(x => string.Equals(x.ReminderPresetId ?? "custom", presetId, StringComparison.OrdinalIgnoreCase));
                int timingSaveCount = reminderTimingSaves.Count(x => string.Equals(x.ReminderPresetId ?? "custom", presetId, StringComparison.OrdinalIgnoreCase));
                FastingTelemetryEventRecord latestEvent = group.OrderByDescending(static x => x.OccurredAtUtc).First();

                return new FastingTelemetryPresetSummaryModel(
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
            .OrderByDescending(static x => x.StartedSessions)
            .ThenByDescending(static x => x.SelectionCount)
            .ThenBy(static x => x.PresetId, StringComparer.OrdinalIgnoreCase)
            .Take(5)];

        return Result.Success(new FastingTelemetrySummaryModel(
            normalizedWindowHours,
            nowUtc,
            startedEvents.Length,
            completedEvents.Length,
            checkInEvents.Length,
            reminderPresetSelections.Length,
            reminderTimingSaves.Length,
            reminderTimingSaves.Count(static x => string.Equals(x.ReminderSource, "preset", StringComparison.OrdinalIgnoreCase)),
            reminderTimingSaves.Count(static x => string.Equals(x.ReminderSource, "manual", StringComparison.OrdinalIgnoreCase)),
            startedEvents.Length > 0 ? Math.Round((double)completedEvents.Length / startedEvents.Length * 100, 1, MidpointRounding.ToEven) : 0,
            startedEvents.Length > 0 ? Math.Round((double)checkInEvents.Length / startedEvents.Length * 100, 1, MidpointRounding.ToEven) : 0,
            completedDurations.Length > 0 ? Math.Round(completedDurations.Average(), 1, MidpointRounding.ToEven) : null,
            checkInEvents.MaxBy(static x => x.OccurredAtUtc)?.OccurredAtUtc,
            events.MaxBy(static x => x.OccurredAtUtc)?.OccurredAtUtc,
            topPresets));
    }
}
