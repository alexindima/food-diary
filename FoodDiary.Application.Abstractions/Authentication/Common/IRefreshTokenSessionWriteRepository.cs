using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Authentication.Common;

public interface IRefreshTokenSessionWriteRepository {
    Task<UserRefreshTokenSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(UserRefreshTokenSession session, CancellationToken cancellationToken = default);

    Task UpdateAsync(UserRefreshTokenSession session, CancellationToken cancellationToken = default);

    Task RevokeAllAsync(UserId userId, DateTime revokedAtUtc, CancellationToken cancellationToken = default);
}
