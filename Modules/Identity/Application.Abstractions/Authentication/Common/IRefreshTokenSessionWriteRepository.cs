using FoodDiary.Modules.Identity.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Identity.Domain.Entities.Users;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;

public interface IRefreshTokenSessionWriteRepository {
    Task<UserRefreshTokenSession?> GetByIdAsync(RefreshTokenSessionId id, CancellationToken cancellationToken = default);

    Task AddAsync(UserRefreshTokenSession session, CancellationToken cancellationToken = default);

    Task UpdateAsync(UserRefreshTokenSession session, CancellationToken cancellationToken = default);

    Task<bool> TryRotateAsync(
        RefreshTokenSessionId id,
        UserId userId,
        string expectedRefreshTokenHash,
        string newRefreshTokenHash,
        bool rememberMe,
        DateTime rotatedAtUtc,
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
