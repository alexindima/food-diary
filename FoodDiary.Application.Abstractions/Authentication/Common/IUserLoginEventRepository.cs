using FoodDiary.Application.Abstractions.Authentication.Models;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Abstractions.Authentication.Common;

public interface IUserLoginEventRepository {
    Task AddAsync(UserLoginEvent loginEvent, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<UserLoginEventReadModel> Items, int TotalItems)> GetPagedAsync(
        int page,
        int limit,
        Guid? userId,
        string? search,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserLoginDeviceSummaryModel>> GetDeviceSummaryAsync(
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default);

    Task<int> DeleteOlderThanAsync(
        DateTime olderThanUtc,
        int batchSize,
        CancellationToken cancellationToken = default);
}
