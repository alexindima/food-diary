using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Fasting.Infrastructure.Persistence;

public sealed class FastingSessionRepository(DbSet<FastingSession> entries, TimeProvider timeProvider) : IFastingSessionRepository {
    public async Task<FastingSession?> GetCurrentAsync(UserId userId, CancellationToken cancellationToken = default) {
        return await entries
            .FirstOrDefaultAsync(s => s.UserId == userId && !s.IsCompleted, cancellationToken).ConfigureAwait(false);
    }

    public async Task<FastingSession?> GetByIdAsync(
        FastingSessionId id, bool asTracking = false, CancellationToken cancellationToken = default) {
        IQueryable<FastingSession> query = entries;

        if (!asTracking) {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(s => s.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<FastingSession>> GetHistoryAsync(
        UserId userId, DateTime from, DateTime to, CancellationToken cancellationToken = default) {
        return await entries
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.StartedAtUtc >= from && s.StartedAtUtc <= to)
            .OrderByDescending(s => s.StartedAtUtc)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> GetCompletedCountAsync(UserId userId, CancellationToken cancellationToken = default) {
        List<FastingSession> endedSessions = await entries
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.IsCompleted && s.EndedAtUtc.HasValue)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return endedSessions.Count(static s => s.IsSuccessfulCompletion);
    }

    public async Task<int> GetCurrentStreakAsync(UserId userId, CancellationToken cancellationToken = default) {
        List<FastingSession> recentSessions = await entries
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.IsCompleted && s.EndedAtUtc.HasValue)
            .OrderByDescending(s => s.StartedAtUtc)
            .Take(60)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        recentSessions = [.. recentSessions.Where(static s => s.IsSuccessfulCompletion)];

        if (recentSessions.Count == 0) {
            return 0;
        }

        int streak = 0;
        DateTime expectedDate = timeProvider.GetUtcNow().UtcDateTime.Date;

        foreach (DateTime sessionDate in recentSessions.Select(session => session.StartedAtUtc.Date)) {
            if (sessionDate == expectedDate || sessionDate == expectedDate.AddDays(-1)) {
                streak++;
                expectedDate = sessionDate.AddDays(-1);
            } else {
                break;
            }
        }

        return streak;
    }

    public Task<FastingSession> AddAsync(FastingSession session, CancellationToken cancellationToken = default) {
        entries.Add(session);
        return Task.FromResult(session);
    }

    public Task UpdateAsync(FastingSession session, CancellationToken cancellationToken = default) {
        if (entries.Entry(session).State == EntityState.Detached) {
            entries.Update(session);
        }

        return Task.CompletedTask;
    }
}
