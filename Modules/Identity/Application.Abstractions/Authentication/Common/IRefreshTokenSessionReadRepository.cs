using FoodDiary.Modules.Identity.Domain.Entities.Users;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;

public interface IRefreshTokenSessionReadRepository {
    Task<UserRefreshTokenSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserRefreshTokenSession>> GetActiveByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);
}
