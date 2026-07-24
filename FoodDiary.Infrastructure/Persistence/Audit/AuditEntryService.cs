using FoodDiary.Application.Abstractions.Audit.Common;
using FoodDiary.Application.Abstractions.Audit.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Audit;

internal sealed class AuditEntryService(FoodDiaryDbContext context, TimeProvider timeProvider)
    : IAuditEntryReadService, IAuditEntryWriter {
    public async Task AddAsync(
        UserId actorUserId,
        Guid? subjectClientUserId,
        string action,
        string targetType,
        string? targetId,
        string? metadata,
        CancellationToken cancellationToken = default) {
        await context.AuditEntries.AddAsync(
            new AuditEntry {
                Id = Guid.NewGuid(),
                ActorUserId = actorUserId.Value,
                SubjectClientUserId = subjectClientUserId,
                Action = action,
                TargetType = targetType,
                TargetId = targetId,
                Metadata = metadata,
                CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime,
            },
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AuditEntryReadModel>> GetRecentAsync(
        Guid? subjectClientUserId,
        int limit,
        CancellationToken cancellationToken = default) {
        IQueryable<AuditEntry> query = context.AuditEntries.AsNoTracking();
        if (subjectClientUserId.HasValue) {
            query = query.Where(entry => entry.SubjectClientUserId == subjectClientUserId.Value);
        }

        return await query
            .OrderByDescending(entry => entry.CreatedAtUtc)
            .Take(limit)
            .Select(entry => new AuditEntryReadModel(
                entry.Id,
                entry.ActorUserId,
                entry.SubjectClientUserId,
                entry.Action,
                entry.TargetType,
                entry.TargetId,
                entry.Metadata,
                entry.CreatedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
