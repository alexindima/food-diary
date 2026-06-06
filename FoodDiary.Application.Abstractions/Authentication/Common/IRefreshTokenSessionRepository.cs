using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Authentication.Common;

public interface IRefreshTokenSessionRepository {
    Task<UserRefreshTokenSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserRefreshTokenSession>> GetActiveByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);
    Task AddAsync(UserRefreshTokenSession session, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserRefreshTokenSession session, CancellationToken cancellationToken = default);
}
