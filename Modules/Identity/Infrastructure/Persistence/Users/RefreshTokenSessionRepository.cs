using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Identity.Infrastructure.Persistence.Users;

public sealed class RefreshTokenSessionRepository(DbSet<UserRefreshTokenSession> sessions, Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade database, Func<CancellationToken, Task>? synchronizeTransactionAsync = null) : IUserSessionRevocationService, IRefreshTokenSessionRepository, IRefreshTokenSessionReadModelRepository {
    public async Task<IReadOnlyList<RefreshTokenSessionReadModel>> GetActiveReadModelsAsync(
        UserId userId, CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        return await sessions
            .AsNoTracking()
            .Where(session => session.UserId == userId && session.RevokedAtUtc == null)
            .OrderByDescending(session => session.LastRotatedAtUtc)
            .Select(session => new RefreshTokenSessionReadModel(
                session.Id, session.AuthProvider, session.UserAgent, session.CreatedAtUtc, session.LastRotatedAtUtc))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<UserRefreshTokenSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        return await sessions
            .FirstOrDefaultAsync(session => session.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<UserRefreshTokenSession>> GetActiveByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        return await sessions
            .Where(session => session.UserId == userId && session.RevokedAtUtc == null)
            .OrderByDescending(session => session.LastRotatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(UserRefreshTokenSession session, CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        await sessions.AddAsync(session, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(UserRefreshTokenSession session, CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        sessions.Update(session);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    public async Task<bool> TryRotateAsync(
        Guid id,
        UserId userId,
        string expectedRefreshTokenHash,
        string newRefreshTokenHash,
        bool rememberMe,
        DateTime rotatedAtUtc,
        CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        if (!database.IsRelational()) {
            UserRefreshTokenSession? session = await sessions
                .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken).ConfigureAwait(false);
            if (session is null || session.UserId != userId || !session.IsActive ||
                !string.Equals(session.RefreshTokenHash, expectedRefreshTokenHash, StringComparison.Ordinal)) {
                return false;
            }

            session.Rotate(newRefreshTokenHash, rememberMe, rotatedAtUtc, TimeSpan.Zero);
            return true;
        }

        int affected = await sessions
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
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        List<UserRefreshTokenSession> activeSessions = await sessions
            .Where(session => session.UserId == userId && session.RevokedAtUtc == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (UserRefreshTokenSession session in activeSessions) {
            session.Revoke(revokedAtUtc);
        }
    }

    public async Task RevokeByIdAsync(
        Guid id,
        UserId userId,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        if (!database.IsRelational()) {
            UserRefreshTokenSession? session = await sessions
                .FirstOrDefaultAsync(candidate => candidate.Id == id && candidate.UserId == userId, cancellationToken)
                .ConfigureAwait(false);
            session?.Revoke(revokedAtUtc);
            return;
        }

        await sessions
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
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        if (!database.IsRelational()) {
            bool currentSessionIsActive = await sessions.AnyAsync(
                session => session.Id == currentSessionId && session.UserId == userId && session.RevokedAtUtc == null,
                cancellationToken).ConfigureAwait(false);
            if (!currentSessionIsActive) {
                return;
            }

            UserRefreshTokenSession? targetSession = await sessions
                .FirstOrDefaultAsync(
                    session => session.Id == id && session.Id != currentSessionId && session.UserId == userId,
                    cancellationToken)
                .ConfigureAwait(false);
            targetSession?.Revoke(revokedAtUtc);
            return;
        }

        await sessions
            .Where(session =>
                session.Id == id &&
                session.Id != currentSessionId &&
                session.UserId == userId &&
                session.RevokedAtUtc == null &&
                sessions.Any(currentSession =>
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
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        if (!database.IsRelational()) {
            bool currentSessionIsActive = await sessions.AnyAsync(
                session => session.Id == currentSessionId && session.UserId == userId && session.RevokedAtUtc == null,
                cancellationToken).ConfigureAwait(false);
            if (!currentSessionIsActive) {
                return;
            }

            List<UserRefreshTokenSession> otherSessions = await sessions
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

        await sessions
            .Where(session =>
                session.UserId == userId &&
                session.Id != currentSessionId &&
                session.RevokedAtUtc == null &&
                sessions.Any(currentSession =>
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
