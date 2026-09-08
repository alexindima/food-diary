using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Authentication.Models;

namespace FoodDiary.Application.Identity.Authentication.Services;

public sealed class AuthenticationLoginEventReadService(IUserLoginEventReadRepository repository)
    : IAuthenticationLoginEventReadService {
    public Task<(IReadOnlyList<UserLoginEventReadModel> Items, int TotalItems)> GetEventsAsync(
        int page,
        int limit,
        Guid? userId,
        string? search,
        CancellationToken cancellationToken, DateTimeOffset? fromUtc = null, DateTimeOffset? toUtc = null, string? provider = null, string? device = null) =>
        repository.GetPagedAsync(page, limit, userId, search, cancellationToken, fromUtc, toUtc, provider, device);

    public Task<IReadOnlyList<UserLoginDeviceSummaryModel>> GetDeviceSummaryAsync(
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken) =>
        repository.GetDeviceSummaryAsync(fromUtc, toUtc, cancellationToken);
}
