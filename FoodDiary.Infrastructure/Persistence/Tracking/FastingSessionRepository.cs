using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Tracking;

public class FastingSessionRepository(FoodDiaryDbContext context) : IFastingSessionRepository {
    public async Task<FastingSession?> GetCurrentAsync(UserId userId, CancellationToken cancellationToken = default) {
        return await context.FastingSessions
            .FirstOrDefaultAsync(s => s.UserId == userId && !s.IsCompleted, cancellationToken).ConfigureAwait(false);
    }

    public async Task<FastingSession?> GetByIdAsync(
        FastingSessionId id, bool asTracking = false, CancellationToken cancellationToken = default) {
        IQueryable<FastingSession> query = context.FastingSessions;

        if (!asTracking) {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(s => s.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<FastingSession>> GetHistoryAsync(
        UserId userId, DateTime from, DateTime to, CancellationToken cancellationToken = default) {
        return await context.FastingSessions
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.StartedAtUtc >= from && s.StartedAtUtc <= to)
            .OrderByDescending(s => s.StartedAtUtc)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> GetCompletedCountAsync(UserId userId, CancellationToken cancellationToken = default) {
        List<FastingSession> endedSessions = await context.FastingSessions
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.IsCompleted && s.EndedAtUtc.HasValue)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return endedSessions.Count(static s => s.IsSuccessfulCompletion);
    }

    public async Task<int> GetCurrentStreakAsync(UserId userId, CancellationToken cancellationToken = default) {
        List<FastingSession> recentSessions = await context.FastingSessions
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.IsCompleted && s.EndedAtUtc.HasValue)
            .OrderByDescending(s => s.StartedAtUtc)
            .Take(60)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        recentSessions = recentSessions
            .Where(static s => s.IsSuccessfulCompletion)
            .ToList();

        if (recentSessions.Count == 0) {
            return 0;
        }

        int streak = 0;
        DateTime expectedDate = DateTime.UtcNow.Date;

        foreach (FastingSession? session in recentSessions) {
            DateTime sessionDate = session.StartedAtUtc.Date;
            if (sessionDate == expectedDate || sessionDate == expectedDate.AddDays(-1)) {
                streak++;
                expectedDate = sessionDate.AddDays(-1);
            } else {
                break;
            }
        }

        return streak;
    }

    public async Task<FastingSession> AddAsync(FastingSession session, CancellationToken cancellationToken = default) {
        context.FastingSessions.Add(session);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return session;
    }

    public async Task UpdateAsync(FastingSession session, CancellationToken cancellationToken = default) {
        context.FastingSessions.Update(session);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
