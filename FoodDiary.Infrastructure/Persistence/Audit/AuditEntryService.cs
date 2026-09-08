using FoodDiary.Application.Abstractions.Audit.Common;
using FoodDiary.Application.Abstractions.Audit.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Audit;

internal sealed class AuditEntryService(FoodDiaryDbContext context, TimeProvider timeProvider)
    : IAuditEntryReadService, IAuditEntryWriter, IAuditEntryJournal {
    public async Task<AuditEntryPage> GetPageAsync(AuditEntryFilter filter, CancellationToken cancellationToken) {
        IQueryable<AuditEntry> query = context.AuditEntries.AsNoTracking();
        if (filter.FromUtc.HasValue) { query = query.Where(entry => entry.CreatedAtUtc >= filter.FromUtc.Value.UtcDateTime); }
        if (filter.ToUtc.HasValue) { query = query.Where(entry => entry.CreatedAtUtc < filter.ToUtc.Value.UtcDateTime); }
        if (filter.ActorUserId.HasValue) { query = query.Where(entry => entry.ActorUserId == filter.ActorUserId); }
        if (filter.SubjectClientUserId.HasValue) { query = query.Where(entry => entry.SubjectClientUserId == filter.SubjectClientUserId); }
        if (!string.IsNullOrWhiteSpace(filter.Action)) { query = query.Where(entry => entry.Action == filter.Action); }
        if (!string.IsNullOrWhiteSpace(filter.TargetType)) { query = query.Where(entry => entry.TargetType == filter.TargetType); }
        if (!string.IsNullOrWhiteSpace(filter.TargetId)) { query = query.Where(entry => entry.TargetId == filter.TargetId); }
        int count = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        List<AuditEntryReadModel> items = await query.OrderByDescending(entry => entry.CreatedAtUtc).ThenByDescending(entry => entry.Id)
            .Skip((filter.Page - 1) * filter.Limit).Take(filter.Limit)
            .Select(entry => new AuditEntryReadModel(entry.Id, entry.ActorUserId, entry.SubjectClientUserId,
                entry.Action, entry.TargetType, entry.TargetId, entry.Metadata, entry.CreatedAtUtc))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return new AuditEntryPage(items, count);
    }

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
