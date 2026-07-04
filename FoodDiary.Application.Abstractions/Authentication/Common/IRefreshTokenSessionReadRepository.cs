using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Authentication.Common;

public interface IRefreshTokenSessionReadRepository {
    Task<UserRefreshTokenSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserRefreshTokenSession>> GetActiveByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);
}
