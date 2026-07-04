using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Abstractions.Authentication.Common;

public interface IUserLoginEventWriteRepository {
    Task AddAsync(UserLoginEvent loginEvent, CancellationToken cancellationToken = default);

    Task<int> DeleteOlderThanAsync(
        DateTime olderThanUtc,
        int batchSize,
        CancellationToken cancellationToken = default);
}
