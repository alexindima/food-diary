using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Wearables.Common;
using FoodDiary.Application.Abstractions.Wearables.Models;
using FoodDiary.Application.Wearables.Common;
using FoodDiary.Domain.Entities.Wearables;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Wearables.Commands.SyncWearableData;

public sealed class SyncWearableDataCommandHandler(
    IEnumerable<IWearableClient> wearableClients,
    IWearableConnectionWriteRepository connectionRepository,
    IWearableSyncWriteRepository syncRepository)
    : ICommandHandler<SyncWearableDataCommand, Result<WearableDailySummaryModel>> {
    public async Task<Result<WearableDailySummaryModel>> Handle(
        SyncWearableDataCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<WearableDailySummaryModel>(userIdResult.Error);
        }

        Result<WearableProvider> providerResult = WearableProviderParser.Parse(command.Provider);
        if (providerResult.IsFailure) {
            return Result.Failure<WearableDailySummaryModel>(providerResult.Error);
        }

        WearableProvider provider = providerResult.Value;

        WearableConnection? connection = await connectionRepository.GetAsync(userIdResult.Value, provider, cancellationToken).ConfigureAwait(false);
        if (connection?.IsActive != true) {
            return Result.Failure<WearableDailySummaryModel>(Errors.Wearable.NotConnected(command.Provider));
        }

        IWearableClient? client = wearableClients.FirstOrDefault(c => c.Provider == provider);
        if (client is null) {
            return Result.Failure<WearableDailySummaryModel>(Errors.Wearable.ProviderNotConfigured(command.Provider));
        }

        // Refresh token if expired
        if (connection.IsTokenExpired() && connection.RefreshToken is not null) {
            WearableTokenResult? refreshResult = await client.RefreshTokenAsync(connection.RefreshToken, cancellationToken).ConfigureAwait(false);
            if (refreshResult is null) {
                connection.Deactivate();
                await connectionRepository.UpdateAsync(connection, cancellationToken).ConfigureAwait(false);
                return Result.Failure<WearableDailySummaryModel>(Errors.Wearable.AuthFailed(command.Provider));
            }
            connection.UpdateTokens(refreshResult.AccessToken, refreshResult.RefreshToken, refreshResult.ExpiresAtUtc);
            await connectionRepository.UpdateAsync(connection, cancellationToken).ConfigureAwait(false);
        }

        IReadOnlyList<WearableDataPoint> dataPoints = await client.FetchDailyDataAsync(connection.AccessToken, command.Date, cancellationToken).ConfigureAwait(false);

        foreach (WearableDataPoint point in dataPoints) {
            WearableSyncEntry? existing = await syncRepository.GetAsync(
                userIdResult.Value, provider, point.DataType, command.Date, cancellationToken).ConfigureAwait(false);

            if (existing is not null) {
                existing.UpdateValue(point.Value);
                await syncRepository.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
            } else {
                var entry = WearableSyncEntry.Create(
                    userIdResult.Value, provider, point.DataType, command.Date, point.Value);
                await syncRepository.AddAsync(entry, cancellationToken).ConfigureAwait(false);
            }
        }

        connection.MarkSynced();
        await connectionRepository.UpdateAsync(connection, cancellationToken).ConfigureAwait(false);

        WearableDailySummaryModel summary = await BuildSummaryAsync(userIdResult.Value, command.Date, cancellationToken).ConfigureAwait(false);
        return Result.Success(summary);
    }

    private async Task<WearableDailySummaryModel> BuildSummaryAsync(
        UserId userId,
        DateTime date,
        CancellationToken cancellationToken) {
        IReadOnlyList<WearableSyncEntry> entries = await syncRepository.GetDailySummaryAsync(userId, date, cancellationToken).ConfigureAwait(false);
        return MapToSummary(date, entries);
    }

    internal static WearableDailySummaryModel MapToSummary(DateTime date, IReadOnlyList<WearableSyncEntry> entries) {
        double? steps = null, heartRate = null, calories = null, active = null, sleep = null;

        foreach (WearableSyncEntry e in entries) {
            switch (e.DataType) {
                case WearableDataType.Steps: steps = (steps ?? 0) + e.Value; break;
                case WearableDataType.HeartRate: heartRate = e.Value; break;
                case WearableDataType.CaloriesBurned: calories = (calories ?? 0) + e.Value; break;
                case WearableDataType.ActiveMinutes: active = (active ?? 0) + e.Value; break;
                case WearableDataType.SleepMinutes: sleep = (sleep ?? 0) + e.Value; break;
            }
        }

        return new WearableDailySummaryModel(date, steps, heartRate, calories, active, sleep);
    }
}
