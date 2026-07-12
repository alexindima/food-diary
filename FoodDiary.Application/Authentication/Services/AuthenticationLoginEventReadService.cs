using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Authentication.Models;
using FoodDiary.Application.Authentication.Common;

namespace FoodDiary.Application.Authentication.Services;

public sealed class AuthenticationLoginEventReadService(IUserLoginEventReadRepository repository)
    : IAuthenticationLoginEventReadService {
    public Task<(IReadOnlyList<UserLoginEventReadModel> Items, int TotalItems)> GetEventsAsync(
        int page,
        int limit,
        Guid? userId,
        string? search,
        CancellationToken cancellationToken) =>
        repository.GetPagedAsync(page, limit, userId, search, cancellationToken);

    public Task<IReadOnlyList<UserLoginDeviceSummaryModel>> GetDeviceSummaryAsync(
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken) =>
        repository.GetDeviceSummaryAsync(fromUtc, toUtc, cancellationToken);
}
