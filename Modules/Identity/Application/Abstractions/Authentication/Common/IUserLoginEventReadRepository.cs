using FoodDiary.Application.Abstractions.Authentication.Models;

namespace FoodDiary.Application.Abstractions.Authentication.Common;

public interface IUserLoginEventReadRepository {
    Task<(IReadOnlyList<UserLoginEventReadModel> Items, int TotalItems)> GetPagedAsync(
        int page,
        int limit,
        Guid? userId,
        string? search,
        CancellationToken cancellationToken = default, DateTimeOffset? fromUtc = null, DateTimeOffset? toUtc = null, string? provider = null, string? device = null);

    Task<IReadOnlyList<UserLoginDeviceSummaryModel>> GetDeviceSummaryAsync(
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default);
}
