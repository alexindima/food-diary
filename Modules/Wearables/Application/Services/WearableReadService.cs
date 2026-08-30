using FoodDiary.Application.Abstractions.Wearables.Common;
using FoodDiary.Application.Abstractions.Wearables.Models;
using FoodDiary.Application.Wearables.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Wearables.Services;

internal sealed class WearableReadService(
    IWearableConnectionReadRepository connectionRepository,
    IWearableSyncReadModelRepository syncRepository) : IWearableReadService {
    public async Task<IReadOnlyList<WearableConnectionModel>> GetConnectionsAsync(
        UserId userId,
        CancellationToken cancellationToken) {
        return await connectionRepository.GetConnectionModelsAsync(userId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<WearableDailySummaryModel> GetDailySummaryAsync(
        UserId userId,
        DateTime date,
        CancellationToken cancellationToken) {
        IReadOnlyList<WearableSyncEntryReadModel> entries = await syncRepository
            .GetDailySummaryReadModelsAsync(userId, date, cancellationToken)
            .ConfigureAwait(false);

        return MapToSummary(date, entries);
    }

    private static WearableDailySummaryModel MapToSummary(DateTime date, IReadOnlyList<WearableSyncEntryReadModel> entries) {
        double? steps = null, heartRate = null, calories = null, active = null, sleep = null;

        foreach (WearableSyncEntryReadModel entry in entries) {
            switch (entry.DataType) {
                case WearableDataType.Steps: steps = (steps ?? 0) + entry.Value; break;
                case WearableDataType.HeartRate: heartRate = entry.Value; break;
                case WearableDataType.CaloriesBurned: calories = (calories ?? 0) + entry.Value; break;
                case WearableDataType.ActiveMinutes: active = (active ?? 0) + entry.Value; break;
                case WearableDataType.SleepMinutes: sleep = (sleep ?? 0) + entry.Value; break;
            }
        }

        return new WearableDailySummaryModel(date, steps, heartRate, calories, active, sleep);
    }
}
