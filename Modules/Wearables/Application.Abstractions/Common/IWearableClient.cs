using FoodDiary.Modules.Wearables.Application.Abstractions.Models;
using FoodDiary.Modules.Wearables.Domain.Enums;
using FoodDiary.Results;

namespace FoodDiary.Modules.Wearables.Application.Abstractions.Common;

public interface IWearableClient {
    WearableProvider Provider { get; }

    string GetAuthorizationUrl(string state);

    Task<WearableTokenResult?> ExchangeCodeAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task<Result<WearableTokenResult>> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<WearableDataPoint>>> FetchDailyDataAsync(
        string accessToken,
        DateTime date,
        CancellationToken cancellationToken = default);
}
