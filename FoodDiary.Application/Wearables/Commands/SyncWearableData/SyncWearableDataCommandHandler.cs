using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Wearables.Common;
using FoodDiary.Application.Abstractions.Wearables.Models;
using FoodDiary.Domain.Entities.Wearables;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Wearables.Commands.SyncWearableData;

public class SyncWearableDataCommandHandler(
    IEnumerable<IWearableClient> wearableClients,
    IWearableConnectionRepository connectionRepository,
    IWearableSyncRepository syncRepository)
    : ICommandHandler<SyncWearableDataCommand, Result<WearableDailySummaryModel>> {
    public async Task<Result<WearableDailySummaryModel>> Handle(
        SyncWearableDataCommand command,
        CancellationToken cancellationToken) {
        var userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<WearableDailySummaryModel>(userIdResult.Error);
        }

        if (!Enum.TryParse<WearableProvider>(command.Provider, true, out var provider)) {
            return Result.Failure<WearableDailySummaryModel>(Errors.Wearable.InvalidProvider(command.Provider));
        }

        var connection = await connectionRepository.GetAsync(userIdResult.Value, provider, cancellationToken);
        if (connection is null || !connection.IsActive) {
            return Result.Failure<WearableDailySummaryModel>(Errors.Wearable.NotConnected(command.Provider));
        }

        var client = wearableClients.FirstOrDefault(c => c.Provider == provider);
        if (client is null) {
            return Result.Failure<WearableDailySummaryModel>(Errors.Wearable.ProviderNotConfigured(command.Provider));
        }

        // Refresh token if expired
        if (connection.IsTokenExpired() && connection.RefreshToken is not null) {
            var refreshResult = await client.RefreshTokenAsync(connection.RefreshToken, cancellationToken);
            if (refreshResult is null) {
                connection.Deactivate();
                await connectionRepository.UpdateAsync(connection, cancellationToken);
                return Result.Failure<WearableDailySummaryModel>(Errors.Wearable.AuthFailed(command.Provider));
            }
            connection.UpdateTokens(refreshResult.AccessToken, refreshResult.RefreshToken, refreshResult.ExpiresAtUtc);
            await connectionRepository.UpdateAsync(connection, cancellationToken);
        }

        var dataPoints = await client.FetchDailyDataAsync(connection.AccessToken, command.Date, cancellationToken);

        foreach (var point in dataPoints) {
            var existing = await syncRepository.GetAsync(
                userIdResult.Value, provider, point.DataType, command.Date, cancellationToken);

            if (existing is not null) {
                existing.UpdateValue(point.Value);
                await syncRepository.UpdateAsync(existing, cancellationToken);
            } else {
                var entry = WearableSyncEntry.Create(
                    userIdResult.Value, provider, point.DataType, command.Date, point.Value);
                await syncRepository.AddAsync(entry, cancellationToken);
            }
        }

        connection.MarkSynced();
        await connectionRepository.UpdateAsync(connection, cancellationToken);

        var summary = await BuildSummaryAsync(userIdResult.Value, command.Date, cancellationToken);
        return Result.Success(summary);
    }

    private async Task<WearableDailySummaryModel> BuildSummaryAsync(
        Domain.ValueObjects.Ids.UserId userId,
        DateTime date,
        CancellationToken cancellationToken) {
        var entries = await syncRepository.GetDailySummaryAsync(userId, date, cancellationToken);
        return MapToSummary(date, entries);
    }

    internal static WearableDailySummaryModel MapToSummary(DateTime date, IReadOnlyList<WearableSyncEntry> entries) {
        double? steps = null, heartRate = null, calories = null, active = null, sleep = null;

        foreach (var e in entries) {
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
