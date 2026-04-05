using FoodDiary.Application.Wearables.Models;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Wearables.Common;

public interface IWearableClient {
    WearableProvider Provider { get; }

    string GetAuthorizationUrl(string state);

    Task<WearableTokenResult?> ExchangeCodeAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task<WearableTokenResult?> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WearableDataPoint>> FetchDailyDataAsync(
        string accessToken,
        DateTime date,
        CancellationToken cancellationToken = default);
}
