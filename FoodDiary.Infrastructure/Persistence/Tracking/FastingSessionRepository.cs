using FoodDiary.Application.Fasting.Common;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Tracking;

public class FastingSessionRepository(FoodDiaryDbContext context) : IFastingSessionRepository {
    public async Task<FastingSession?> GetCurrentAsync(UserId userId, CancellationToken cancellationToken = default) {
        return await context.FastingSessions
            .FirstOrDefaultAsync(s => s.UserId == userId && !s.IsCompleted, cancellationToken);
    }

    public async Task<FastingSession?> GetByIdAsync(
        FastingSessionId id, bool asTracking = false, CancellationToken cancellationToken = default) {
        IQueryable<FastingSession> query = context.FastingSessions;

        if (!asTracking) {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<FastingSession>> GetHistoryAsync(
        UserId userId, DateTime from, DateTime to, CancellationToken cancellationToken = default) {
        return await context.FastingSessions
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.StartedAtUtc >= from && s.StartedAtUtc <= to)
            .OrderByDescending(s => s.StartedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCompletedCountAsync(UserId userId, CancellationToken cancellationToken = default) {
        return await context.FastingSessions
            .CountAsync(s => s.UserId == userId && s.IsCompleted, cancellationToken);
    }

    public async Task<int> GetCurrentStreakAsync(UserId userId, CancellationToken cancellationToken = default) {
        var recentSessions = await context.FastingSessions
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.IsCompleted)
            .OrderByDescending(s => s.StartedAtUtc)
            .Take(60)
            .ToListAsync(cancellationToken);

        if (recentSessions.Count == 0) {
            return 0;
        }

        var streak = 0;
        var expectedDate = DateTime.UtcNow.Date;

        foreach (var session in recentSessions) {
            var sessionDate = session.StartedAtUtc.Date;
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
        await context.SaveChangesAsync(cancellationToken);
        return session;
    }

    public async Task UpdateAsync(FastingSession session, CancellationToken cancellationToken = default) {
        context.FastingSessions.Update(session);
        await context.SaveChangesAsync(cancellationToken);
    }
}
