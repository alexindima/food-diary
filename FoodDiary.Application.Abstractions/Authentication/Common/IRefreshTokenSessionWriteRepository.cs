using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Abstractions.Authentication.Common;

public interface IRefreshTokenSessionWriteRepository {
    Task<UserRefreshTokenSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(UserRefreshTokenSession session, CancellationToken cancellationToken = default);

    Task UpdateAsync(UserRefreshTokenSession session, CancellationToken cancellationToken = default);
}
