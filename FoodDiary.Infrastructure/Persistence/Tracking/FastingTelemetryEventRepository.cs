using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Tracking;

public sealed class FastingTelemetryEventRepository(FoodDiaryDbContext context) : IFastingTelemetryEventRepository {
    public Task AddAsync(FastingTelemetryEventRecord record, CancellationToken cancellationToken = default) {
        var entity = FastingTelemetryEvent.Create(
            record.Name,
            record.OccurredAtUtc,
            record.SessionId,
            record.Protocol,
            record.PlanType,
            record.Status,
            record.OccurrenceKind,
            record.ReminderPresetId,
            record.ReminderSource,
            record.FirstReminderHours,
            record.FollowUpReminderHours,
            record.PlannedDurationHours,
            record.ActualDurationHours,
            record.HungerLevel,
            record.EnergyLevel,
            record.MoodLevel,
            record.SymptomsCount,
            record.HadNotes);

        context.FastingTelemetryEvents.Add(entity);
        return Task.CompletedTask;
    }

    public async Task<int> DeleteOlderThanAsync(
        DateTime olderThanUtc,
        int batchSize,
        CancellationToken cancellationToken = default) {
        FastingTelemetryEventId[] ids = await context.FastingTelemetryEvents
            .AsNoTracking()
            .Where(item => item.OccurredAtUtc < olderThanUtc)
            .OrderBy(item => item.OccurredAtUtc)
            .Select(item => item.Id)
            .Take(Math.Max(batchSize, 1))
            .ToArrayAsync(cancellationToken).ConfigureAwait(false);

        if (ids.Length == 0) {
            return 0;
        }

        return await context.FastingTelemetryEvents
            .Where(item => Enumerable.Contains(ids, item.Id))
            .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<FastingTelemetryEventRecord>> GetRangeAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default) {
        return await context.FastingTelemetryEvents
            .AsNoTracking()
            .Where(x => x.OccurredAtUtc >= fromUtc && x.OccurredAtUtc <= toUtc)
            .OrderByDescending(x => x.OccurredAtUtc)
            .Select(x => new FastingTelemetryEventRecord(
                x.Name,
                x.OccurredAtUtc,
                x.SessionId,
                x.Protocol,
                x.PlanType,
                x.Status,
                x.OccurrenceKind,
                x.ReminderPresetId,
                x.ReminderSource,
                x.FirstReminderHours,
                x.FollowUpReminderHours,
                x.PlannedDurationHours,
                x.ActualDurationHours,
                x.HungerLevel,
                x.EnergyLevel,
                x.MoodLevel,
                x.SymptomsCount,
                x.HadNotes))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
