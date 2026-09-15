using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Wearables.Application.Abstractions.Common;
using FoodDiary.Modules.Wearables.Application.Abstractions.Models;
using FoodDiary.Modules.Wearables.Application.Common;
using FoodDiary.Modules.Wearables.Domain.Entities;
using FoodDiary.Modules.Wearables.Domain.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Wearables.Domain.ValueObjects;

namespace FoodDiary.Modules.Wearables.Application.Commands.SyncWearableData;

public sealed class SyncWearableDataCommandHandler(
    IEnumerable<IWearableClient> wearableClients,
    IWearableConnectionWriteRepository connectionRepository,
    IWearableSyncWriteRepository syncRepository,
    IWearableTransactionRunner transactionRunner,
    ICurrentUserAccessService currentUserAccessService,
    IWearableTokenProtector tokenProtector,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SyncWearableDataCommand, Result<WearableDailySummaryModel>> {
    public async Task<Result<WearableDailySummaryModel>> Handle(
        SyncWearableDataCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<WearableDailySummaryModel>(userIdResult);
        }

        Result<WearableProvider> providerResult = WearableProviderParser.Parse(command.Provider);
        if (providerResult.IsFailure) {
            return Result.Failure<WearableDailySummaryModel>(providerResult.Error);
        }

        WearableProvider provider = providerResult.Value;

        IWearableClient? client = wearableClients.FirstOrDefault(c => c.Provider == provider);
        if (client is null) {
            return Result.Failure<WearableDailySummaryModel>(WearableErrors.ProviderNotConfigured(command.Provider));
        }

        string serializationKey = FormattableString.Invariant(
            $"wearable-sync:{userIdResult.Value.Value:N}:{provider}:{command.Date.Date:yyyy-MM-dd}");
        Result<bool> syncResult = await transactionRunner.ExecuteSerializedAsync(
            serializationKey,
            token => SynchronizeAsync(command, userIdResult.Value, provider, client, token),
            cancellationToken).ConfigureAwait(false);
        if (syncResult.IsFailure) {
            return Result.Failure<WearableDailySummaryModel>(syncResult.Error);
        }

        WearableDailySummaryModel summary = await BuildSummaryAsync(userIdResult.Value, command.Date, cancellationToken).ConfigureAwait(false);
        return Result.Success(summary);
    }

    private async Task<Result<bool>> SynchronizeAsync(
        SyncWearableDataCommand command,
        UserId userId,
        WearableProvider provider,
        IWearableClient client,
        CancellationToken cancellationToken) {
        WearableConnection? connection = await connectionRepository.GetAsync(userId, provider, cancellationToken).ConfigureAwait(false);
        if (connection?.IsActive != true) {
            return Result.Failure<bool>(WearableErrors.NotConnected(command.Provider));
        }

        // Refresh token if expired
        if (connection.IsTokenExpired() && connection.RefreshToken is not null) {
            string refreshToken = tokenProtector.Unprotect(connection.RefreshToken.Value);
            WearableTokenResult? refreshResult = await client.RefreshTokenAsync(refreshToken, cancellationToken).ConfigureAwait(false);
            if (refreshResult is null) {
                connection.Deactivate();
                await PersistConnectionAsync(connection, cancellationToken).ConfigureAwait(false);
                return Result.Failure<bool>(WearableErrors.AuthFailed(command.Provider));
            }
            ProtectedWearableToken protectedAccessToken = tokenProtector.Protect(refreshResult.AccessToken);
            ProtectedWearableToken? protectedRefreshToken = refreshResult.RefreshToken is null ? null : tokenProtector.Protect(refreshResult.RefreshToken);
            connection.UpdateTokens(protectedAccessToken, protectedRefreshToken, refreshResult.ExpiresAtUtc);
            await PersistConnectionAsync(connection, cancellationToken).ConfigureAwait(false);
        }

        string accessToken = tokenProtector.Unprotect(connection.AccessToken);
        Result<IReadOnlyList<WearableDataPoint>> dataResult = await client
            .FetchDailyDataAsync(accessToken, command.Date, cancellationToken)
            .ConfigureAwait(false);
        if (dataResult.IsFailure) {
            return Result.Failure<bool>(dataResult.Error);
        }

        ProtectLegacyTokens(connection, accessToken);

        await StoreDataPointsAsync(userId, provider, command.Date, dataResult.Value, cancellationToken).ConfigureAwait(false);

        connection.MarkSynced();
        await connectionRepository.UpdateAsync(connection, cancellationToken).ConfigureAwait(false);

        return Result.Success(value: true);
    }

    private async Task PersistConnectionAsync(
        WearableConnection connection,
        CancellationToken cancellationToken) {
        await connectionRepository.UpdateAsync(connection, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private void ProtectLegacyTokens(WearableConnection connection, string accessToken) {
        if (connection.AccessToken.IsProtected &&
            connection.RefreshToken?.IsProtected != false) {
            return;
        }

        connection.UpdateTokens(
            tokenProtector.Protect(accessToken),
            connection.RefreshToken is null ? null : tokenProtector.Protect(tokenProtector.Unprotect(connection.RefreshToken.Value)),
            connection.TokenExpiresAtUtc);
    }

    private async Task StoreDataPointsAsync(
        UserId userId,
        WearableProvider provider,
        DateTime date,
        IReadOnlyList<WearableDataPoint> dataPoints,
        CancellationToken cancellationToken) {
        foreach (WearableDataPoint point in dataPoints) {
            WearableSyncEntry? existing = await syncRepository
                .GetAsync(userId, provider, point.DataType, date, cancellationToken)
                .ConfigureAwait(false);
            if (existing is not null) {
                existing.UpdateValue(point.Value);
                await syncRepository.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
                continue;
            }

            var entry = WearableSyncEntry.Create(userId, provider, point.DataType, date, point.Value);
            await syncRepository.AddAsync(entry, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<WearableDailySummaryModel> BuildSummaryAsync(
        UserId userId,
        DateTime date,
        CancellationToken cancellationToken) {
        IReadOnlyList<WearableSyncEntry> entries = await syncRepository.GetDailySummaryAsync(userId, date, cancellationToken).ConfigureAwait(false);
        return WearableSummaryCalculator.Calculate(date, entries.Select(entry => (entry.DataType, entry.Value)));
    }

}
