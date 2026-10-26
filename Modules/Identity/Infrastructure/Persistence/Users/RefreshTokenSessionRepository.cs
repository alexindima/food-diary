using Microsoft.EntityFrameworkCore.Infrastructure;
using FoodDiary.Modules.Identity.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Identity.Domain.Entities.Users;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using FoodDiary.Authentication.Contracts.Options;

namespace FoodDiary.Modules.Identity.Infrastructure.Persistence.Users;

public sealed class RefreshTokenSessionRepository(
    DbSet<UserRefreshTokenSession> sessions,
    DatabaseFacade database,
    TimeProvider timeProvider,
    IOptions<JwtOptions> jwtOptions,
    Func<CancellationToken, Task>? synchronizeTransactionAsync = null) : IUserSessionRevocationService, IRefreshTokenSessionRepository, IRefreshTokenSessionReadModelRepository {
    public async Task<BrowserSessionProof?> ReadBrowserSessionProofAsync(RefreshTokenSessionId id, CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        return await sessions.AsNoTracking().Where(session => session.Id == id)
            .Select(session => new BrowserSessionProof(session.Id, session.UserId, session.RefreshTokenHash,
                session.BrowserSecurityVersion, session.ExpiresAtUtc, session.RevokedAtUtc, session.LastRotatedAtUtc,
                session.CreatedAtUtc, session.BrowserSlidingExpirationEnabled))
            .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> TryRecordBrowserActivityAsync(BrowserSessionProof proof, DateTime nowUtc, DateTime lastActiveBeforeUtc, CancellationToken cancellationToken = default) {
        if (proof.SecurityVersion is not { } version) {
            return false;
        }
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        int affected = await sessions.Where(session => session.Id == proof.Id && session.UserId == proof.UserId &&
                session.RevokedAtUtc == null && session.ExpiresAtUtc > nowUtc && session.BrowserSecurityVersion == version &&
                session.RefreshTokenHash == proof.CredentialHash && session.LastRotatedAtUtc <= lastActiveBeforeUtc)
            .ExecuteUpdateAsync(setters => setters.SetProperty(session => session.LastRotatedAtUtc, nowUtc)
                .SetProperty(session => session.ModifiedOnUtc, nowUtc), cancellationToken).ConfigureAwait(false);
        return affected == 1;
    }

    public async Task<bool> TryRenewBrowserSessionAsync(BrowserSessionProof proof, DateTime nowUtc, DateTime expiresAtUtc, CancellationToken cancellationToken = default) {
        if (proof.SecurityVersion is not { } version || proof.ExpiresAtUtc is not { } previous || previous <= nowUtc || expiresAtUtc <= previous) {
            return false;
        }
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        int affected = await sessions.Where(session => session.Id == proof.Id && session.UserId == proof.UserId &&
                session.BrowserSlidingExpirationEnabled && session.RevokedAtUtc == null && session.ExpiresAtUtc == previous &&
                session.BrowserSecurityVersion == version && session.RefreshTokenHash == proof.CredentialHash)
            .ExecuteUpdateAsync(setters => setters.SetProperty(session => session.ExpiresAtUtc, expiresAtUtc)
                .SetProperty(session => session.ModifiedOnUtc, nowUtc), cancellationToken).ConfigureAwait(false);
        return affected == 1;
    }

    public async Task<int> DeleteInactiveBeforeAsync(DateTime olderThanUtc, int batchSize, CancellationToken cancellationToken = default) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(batchSize, 1000);
        if (olderThanUtc.Kind != DateTimeKind.Utc || olderThanUtc > timeProvider.GetUtcNow().UtcDateTime) {
            throw new ArgumentOutOfRangeException(nameof(olderThanUtc), "Session retention requires a past UTC cutoff.");
        }
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        JwtOptions options = jwtOptions.Value;
        DateTime regularCutoff = olderThanUtc.AddDays(-(options.RefreshTokenExpirationDays > 0 ? options.RefreshTokenExpirationDays : 7));
        DateTime rememberedCutoff = olderThanUtc.AddDays(-(options.RememberMeRefreshTokenExpirationDays > 0 ? options.RememberMeRefreshTokenExpirationDays : 90));
        return await sessions.Where(session => session.RevokedAtUtc <= olderThanUtc || session.ExpiresAtUtc <= olderThanUtc ||
                (session.ExpiresAtUtc == null &&
                    ((session.RememberMe && session.LastRotatedAtUtc <= rememberedCutoff) ||
                     (!session.RememberMe && session.LastRotatedAtUtc <= regularCutoff))))
            .OrderBy(session => session.CreatedAtUtc).ThenBy(session => session.Id).Take(batchSize)
            .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }

    private IQueryable<UserRefreshTokenSession> GetUnexpiredSessions(UserId userId) {
        DateTime nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        JwtOptions options = jwtOptions.Value;
        DateTime regularCutoff = nowUtc.AddDays(-(options.RefreshTokenExpirationDays > 0 ? options.RefreshTokenExpirationDays : 7));
        DateTime rememberedCutoff = nowUtc.AddDays(-(options.RememberMeRefreshTokenExpirationDays > 0 ? options.RememberMeRefreshTokenExpirationDays : 90));
        return sessions.Where(session => session.UserId == userId && session.RevokedAtUtc == null &&
            (session.ExpiresAtUtc > nowUtc || (session.ExpiresAtUtc == null &&
                ((session.RememberMe && session.LastRotatedAtUtc > rememberedCutoff) ||
                 (!session.RememberMe && session.LastRotatedAtUtc > regularCutoff)))));
    }

    public async Task<IReadOnlyList<RefreshTokenSessionReadModel>> GetActiveReadModelsAsync(
        UserId userId, CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        return await GetUnexpiredSessions(userId)
            .AsNoTracking()
            .OrderByDescending(session => session.LastRotatedAtUtc)
            .Select(session => new RefreshTokenSessionReadModel(
                session.Id, session.AuthProvider, session.UserAgent, session.CreatedAtUtc, session.LastRotatedAtUtc))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<RefreshTokenSessionReadModel>> GetActivePageReadModelsAsync(
        UserId userId,
        int page,
        int limit,
        CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        return await GetUnexpiredSessions(userId)
            .AsNoTracking()
            .OrderByDescending(session => session.LastRotatedAtUtc)
            .ThenBy(session => session.Id)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(session => new RefreshTokenSessionReadModel(
                session.Id, session.AuthProvider, session.UserAgent, session.CreatedAtUtc, session.LastRotatedAtUtc))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> IsActiveAsync(
        UserId userId,
        RefreshTokenSessionId sessionId,
        CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        return await GetUnexpiredSessions(userId).AsNoTracking().AnyAsync(
            session => session.Id == sessionId,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<UserRefreshTokenSession?> GetByIdAsync(RefreshTokenSessionId id, CancellationToken cancellationToken = default) {
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
        return await GetUnexpiredSessions(userId)
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

    public async Task<bool> TryRevalidateBrowserSessionAsync(
        RefreshTokenSessionId id,
        UserId userId,
        string expectedCredentialHash,
        long securityVersion,
        DateTime nowUtc,
        CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        if (!database.IsRelational()) {
            UserRefreshTokenSession? session = await sessions.FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken).ConfigureAwait(false);
            if (session is null || session.UserId != userId || !session.IsActive ||
                session.BrowserSecurityVersion is not { } version || version > securityVersion ||
                session.ExpiresAtUtc is not { } expires || expires <= nowUtc ||
                !string.Equals(session.RefreshTokenHash, expectedCredentialHash, StringComparison.Ordinal)) {
                return false;
            }
            session.RefreshBrowserSession(securityVersion, nowUtc < session.LastRotatedAtUtc ? session.LastRotatedAtUtc : nowUtc);
            return true;
        }
        int affected = await sessions.Where(session => session.Id == id && session.UserId == userId &&
                session.RevokedAtUtc == null && session.ExpiresAtUtc > nowUtc &&
                session.BrowserSecurityVersion != null && session.BrowserSecurityVersion <= securityVersion &&
                session.RefreshTokenHash == expectedCredentialHash)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(session => session.BrowserSecurityVersion, securityVersion)
                .SetProperty(session => session.LastRotatedAtUtc, session => session.LastRotatedAtUtc < nowUtc ? nowUtc : session.LastRotatedAtUtc)
                .SetProperty(session => session.ModifiedOnUtc, session => session.LastRotatedAtUtc < nowUtc ? nowUtc : session.LastRotatedAtUtc),
                cancellationToken).ConfigureAwait(false);
        return affected == 1;
    }

    public async Task<bool> TryRotateAsync(
        RefreshTokenSessionId id,
        UserId userId,
        string expectedRefreshTokenHash,
        string newRefreshTokenHash,
        bool rememberMe,
        DateTime rotatedAtUtc,
        DateTime? expiresAtUtc = null,
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

            session.Rotate(newRefreshTokenHash, rememberMe, rotatedAtUtc, TimeSpan.Zero, expiresAtUtc);
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
                    .SetProperty(session => session.ExpiresAtUtc, expiresAtUtc)
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
        RefreshTokenSessionId id,
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
        RefreshTokenSessionId id,
        UserId userId,
        RefreshTokenSessionId currentSessionId,
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
        RefreshTokenSessionId currentSessionId,
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
