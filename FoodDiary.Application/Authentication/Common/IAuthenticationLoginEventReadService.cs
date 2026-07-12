using FoodDiary.Application.Abstractions.Authentication.Models;

namespace FoodDiary.Application.Authentication.Common;

public interface IAuthenticationLoginEventReadService {
    Task<(IReadOnlyList<UserLoginEventReadModel> Items, int TotalItems)> GetEventsAsync(
        int page,
        int limit,
        Guid? userId,
        string? search,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<UserLoginDeviceSummaryModel>> GetDeviceSummaryAsync(
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken);
}
