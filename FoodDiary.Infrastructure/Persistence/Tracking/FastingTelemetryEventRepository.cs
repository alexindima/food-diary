using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Tracking;

public sealed class FastingTelemetryEventRepository(FoodDiaryDbContext context) : IFastingTelemetryEventRepository {
    public async Task AddAsync(FastingTelemetryEventRecord record, CancellationToken cancellationToken = default) {
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
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FastingTelemetryEventRecord>> GetSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken = default) {
        return await context.FastingTelemetryEvents
            .AsNoTracking()
            .Where(x => x.OccurredAtUtc >= sinceUtc)
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
            .ToListAsync(cancellationToken);
    }
}
