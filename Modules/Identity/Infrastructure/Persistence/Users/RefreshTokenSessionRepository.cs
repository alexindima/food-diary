using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Users;

public sealed class RefreshTokenSessionRepository(FoodDiaryDbContext context) : IRefreshTokenSessionRepository {
    public Task<UserRefreshTokenSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.UserRefreshTokenSessions
            .FirstOrDefaultAsync(session => session.Id == id, cancellationToken);

    public async Task<IReadOnlyList<UserRefreshTokenSession>> GetActiveByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        return await context.UserRefreshTokenSessions
            .Where(session => session.UserId == userId && session.RevokedAtUtc == null)
            .OrderByDescending(session => session.LastRotatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(UserRefreshTokenSession session, CancellationToken cancellationToken = default) {
        await context.UserRefreshTokenSessions.AddAsync(session, cancellationToken).ConfigureAwait(false);
    }

    public Task UpdateAsync(UserRefreshTokenSession session, CancellationToken cancellationToken = default) {
        context.UserRefreshTokenSessions.Update(session);
        return Task.CompletedTask;
    }

    public async Task<bool> TryRotateAsync(
        Guid id,
        UserId userId,
        string expectedRefreshTokenHash,
        string newRefreshTokenHash,
        bool rememberMe,
        DateTime rotatedAtUtc,
        CancellationToken cancellationToken = default) {
        if (!context.Database.IsRelational()) {
            UserRefreshTokenSession? session = await context.UserRefreshTokenSessions
                .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken).ConfigureAwait(false);
            if (session is null || session.UserId != userId || !session.IsActive ||
                !string.Equals(session.RefreshTokenHash, expectedRefreshTokenHash, StringComparison.Ordinal)) {
                return false;
            }

            session.Rotate(newRefreshTokenHash, rememberMe, rotatedAtUtc, TimeSpan.Zero);
            return true;
        }

        int affected = await context.UserRefreshTokenSessions
            .Where(session =>
                session.Id == id &&
                session.UserId == userId &&
                session.RevokedAtUtc == null &&
                session.RefreshTokenHash == expectedRefreshTokenHash)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(session => session.PreviousRefreshTokenHash, expectedRefreshTokenHash)
                    .SetProperty(session => session.PreviousRefreshTokenValidUntilUtc, (DateTime?)null)
                    .SetProperty(session => session.RefreshTokenHash, newRefreshTokenHash)
                    .SetProperty(session => session.RememberMe, rememberMe)
                    .SetProperty(session => session.LastRotatedAtUtc, rotatedAtUtc),
                cancellationToken)
            .ConfigureAwait(false);
        return affected == 1;
    }

    public async Task RevokeAllAsync(
        UserId userId,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken = default) {
        List<UserRefreshTokenSession> sessions = await context.UserRefreshTokenSessions
            .Where(session => session.UserId == userId && session.RevokedAtUtc == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (UserRefreshTokenSession session in sessions) {
            session.Revoke(revokedAtUtc);
        }
    }

    public async Task RevokeByIdAsync(
        Guid id,
        UserId userId,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken = default) {
        if (!context.Database.IsRelational()) {
            UserRefreshTokenSession? session = await context.UserRefreshTokenSessions
                .FirstOrDefaultAsync(candidate => candidate.Id == id && candidate.UserId == userId, cancellationToken)
                .ConfigureAwait(false);
            session?.Revoke(revokedAtUtc);
            return;
        }

        await context.UserRefreshTokenSessions
            .Where(session =>
                session.Id == id &&
                session.UserId == userId &&
                session.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(session => session.RevokedAtUtc, revokedAtUtc)
                    .SetProperty(session => session.PreviousRefreshTokenHash, (string?)null)
                    .SetProperty(session => session.PreviousRefreshTokenValidUntilUtc, (DateTime?)null),
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task RevokeOtherByIdAsync(
        Guid id,
        UserId userId,
        Guid currentSessionId,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken = default) {
        if (!context.Database.IsRelational()) {
            bool currentSessionIsActive = await context.UserRefreshTokenSessions.AnyAsync(
                session => session.Id == currentSessionId && session.UserId == userId && session.RevokedAtUtc == null,
                cancellationToken).ConfigureAwait(false);
            if (!currentSessionIsActive) {
                return;
            }

            UserRefreshTokenSession? targetSession = await context.UserRefreshTokenSessions
                .FirstOrDefaultAsync(
                    session => session.Id == id && session.Id != currentSessionId && session.UserId == userId,
                    cancellationToken)
                .ConfigureAwait(false);
            targetSession?.Revoke(revokedAtUtc);
            return;
        }

        await context.UserRefreshTokenSessions
            .Where(session =>
                session.Id == id &&
                session.Id != currentSessionId &&
                session.UserId == userId &&
                session.RevokedAtUtc == null &&
                context.UserRefreshTokenSessions.Any(currentSession =>
                    currentSession.Id == currentSessionId &&
                    currentSession.UserId == userId &&
                    currentSession.RevokedAtUtc == null))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(session => session.RevokedAtUtc, revokedAtUtc)
                    .SetProperty(session => session.PreviousRefreshTokenHash, (string?)null)
                    .SetProperty(session => session.PreviousRefreshTokenValidUntilUtc, (DateTime?)null),
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task RevokeAllOtherAsync(
        UserId userId,
        Guid currentSessionId,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken = default) {
        if (!context.Database.IsRelational()) {
            bool currentSessionIsActive = await context.UserRefreshTokenSessions.AnyAsync(
                session => session.Id == currentSessionId && session.UserId == userId && session.RevokedAtUtc == null,
                cancellationToken).ConfigureAwait(false);
            if (!currentSessionIsActive) {
                return;
            }

            List<UserRefreshTokenSession> otherSessions = await context.UserRefreshTokenSessions
                .Where(session =>
                    session.UserId == userId &&
                    session.Id != currentSessionId &&
                    session.RevokedAtUtc == null)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            foreach (UserRefreshTokenSession session in otherSessions) {
                session.Revoke(revokedAtUtc);
            }
            return;
        }

        await context.UserRefreshTokenSessions
            .Where(session =>
                session.UserId == userId &&
                session.Id != currentSessionId &&
                session.RevokedAtUtc == null &&
                context.UserRefreshTokenSessions.Any(currentSession =>
                    currentSession.Id == currentSessionId &&
                    currentSession.UserId == userId &&
                    currentSession.RevokedAtUtc == null))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(session => session.RevokedAtUtc, revokedAtUtc)
                    .SetProperty(session => session.PreviousRefreshTokenHash, (string?)null)
                    .SetProperty(session => session.PreviousRefreshTokenValidUntilUtc, (DateTime?)null),
                cancellationToken)
            .ConfigureAwait(false);
    }
}
