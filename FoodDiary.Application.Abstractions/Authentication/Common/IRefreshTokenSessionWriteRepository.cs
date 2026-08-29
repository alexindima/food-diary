using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Authentication.Common;

public interface IRefreshTokenSessionWriteRepository {
    Task<UserRefreshTokenSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(UserRefreshTokenSession session, CancellationToken cancellationToken = default);

    Task UpdateAsync(UserRefreshTokenSession session, CancellationToken cancellationToken = default);

    Task<bool> TryRotateAsync(
        Guid id,
        UserId userId,
        string expectedRefreshTokenHash,
        string newRefreshTokenHash,
        bool rememberMe,
        DateTime rotatedAtUtc,
        CancellationToken cancellationToken = default);

    Task RevokeAllAsync(UserId userId, DateTime revokedAtUtc, CancellationToken cancellationToken = default);

    Task RevokeByIdAsync(
        Guid id,
        UserId userId,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken = default);

    Task RevokeOtherByIdAsync(
        Guid id,
        UserId userId,
        Guid currentSessionId,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken = default);

    Task RevokeAllOtherAsync(
        UserId userId,
        Guid currentSessionId,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken = default);
}
