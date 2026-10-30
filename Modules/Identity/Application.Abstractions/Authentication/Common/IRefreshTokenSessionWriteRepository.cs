using FoodDiary.Modules.Identity.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Identity.Domain.Entities.Users;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;

namespace FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;

public interface IRefreshTokenSessionWriteRepository {
    Task<int> DeleteInactiveBeforeAsync(DateTime olderThanUtc, int batchSize, CancellationToken cancellationToken = default);

    Task<BrowserSessionProof?> ReadBrowserSessionProofAsync(RefreshTokenSessionId id, CancellationToken cancellationToken = default);

    Task<bool> TryRecordBrowserActivityAsync(BrowserSessionProof proof, DateTime nowUtc, DateTime lastActiveBeforeUtc, CancellationToken cancellationToken = default);

    Task<bool> TryRenewBrowserSessionAsync(BrowserSessionProof proof, DateTime nowUtc, DateTime expiresAtUtc, CancellationToken cancellationToken = default);

    Task<bool> TryRememberBrowserSessionAsync(BrowserSessionProof proof, DateTime nowUtc, CancellationToken cancellationToken = default);

    Task<UserRefreshTokenSession?> GetByIdAsync(RefreshTokenSessionId id, CancellationToken cancellationToken = default);

    Task AddAsync(UserRefreshTokenSession session, CancellationToken cancellationToken = default);

    Task UpdateAsync(UserRefreshTokenSession session, CancellationToken cancellationToken = default);

    Task<bool> TryRevalidateBrowserSessionAsync(
        RefreshTokenSessionId id,
        UserId userId,
        string expectedCredentialHash,
        long securityVersion,
        DateTime nowUtc,
        CancellationToken cancellationToken = default);

    Task<bool> TryRotateAsync(
        RefreshTokenSessionId id,
        UserId userId,
        string expectedRefreshTokenHash,
        string newRefreshTokenHash,
        bool rememberMe,
        DateTime rotatedAtUtc,
        DateTime? expiresAtUtc = null,
        CancellationToken cancellationToken = default);

    Task RevokeAllAsync(UserId userId, DateTime revokedAtUtc, CancellationToken cancellationToken = default);

    Task RevokeByIdAsync(
        RefreshTokenSessionId id,
        UserId userId,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken = default);

    Task RevokeOtherByIdAsync(
        RefreshTokenSessionId id,
        UserId userId,
        RefreshTokenSessionId currentSessionId,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken = default);

    Task RevokeAllOtherAsync(
        UserId userId,
        RefreshTokenSessionId currentSessionId,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken = default);
}
