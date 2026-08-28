using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Wearables.Common;
using FoodDiary.Application.Abstractions.Wearables.Models;
using FoodDiary.Application.Wearables.Common;
using FoodDiary.Domain.Entities.Wearables;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Wearables.Commands.ConnectWearable;

public sealed class ConnectWearableCommandHandler(
    IEnumerable<IWearableClient> wearableClients,
    IWearableConnectionWriteRepository connectionRepository,
    IWearableTransactionRunner transactionRunner,
    IWearableOAuthStateService stateService,
    ICurrentUserAccessService currentUserAccessService,
    IWearableTokenProtector tokenProtector)
    : ICommandHandler<ConnectWearableCommand, Result<WearableConnectionModel>> {
    public async Task<Result<WearableConnectionModel>> Handle(
        ConnectWearableCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<WearableConnectionModel>(userIdResult);
        }

        Result<WearableProvider> providerResult = WearableProviderParser.Parse(command.Provider);
        if (providerResult.IsFailure) {
            return Result.Failure<WearableConnectionModel>(providerResult.Error);
        }

        WearableProvider provider = providerResult.Value;

        string serializationKey = $"wearable-connect:{userIdResult.Value.Value:N}:{provider}";
        return await transactionRunner.ExecuteSerializedAsync(
            serializationKey,
            token => ConnectAsync(command, userIdResult.Value, provider, token),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<Result<WearableConnectionModel>> ConnectAsync(
        ConnectWearableCommand command,
        UserId userId,
        WearableProvider provider,
        CancellationToken cancellationToken) {
        WearableConnection? existing = await connectionRepository
            .GetAsync(userId, provider, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null && string.Equals(existing.LastConnectRequestId, command.RequestId, StringComparison.Ordinal)) {
            return string.Equals(existing.LastConnectRequestHash, command.RequestHash, StringComparison.Ordinal)
                ? Result.Success(ToModel(existing))
                : Result.Failure<WearableConnectionModel>(WearableErrors.IdempotencyConflict);
        }

        IWearableClient? client = wearableClients.FirstOrDefault(c => c.Provider == provider);
        if (client is null) {
            return Result.Failure<WearableConnectionModel>(Errors.Wearable.ProviderNotConfigured(command.Provider));
        }

        if (!stateService.IsValidState(command.State, userId, provider)) {
            return Result.Failure<WearableConnectionModel>(Errors.Wearable.InvalidState);
        }

        WearableTokenResult? tokenResult = await client.ExchangeCodeAsync(command.Code, cancellationToken).ConfigureAwait(false);
        if (tokenResult is null) {
            return Result.Failure<WearableConnectionModel>(Errors.Wearable.AuthFailed(command.Provider));
        }

        ProtectedWearableToken protectedAccessToken = tokenProtector.Protect(tokenResult.AccessToken);
        ProtectedWearableToken? protectedRefreshToken = tokenResult.RefreshToken is null ? null : tokenProtector.Protect(tokenResult.RefreshToken);
        if (existing is not null) {
            existing.Reconnect(
                tokenResult.ExternalUserId,
                protectedAccessToken,
                protectedRefreshToken,
                tokenResult.ExpiresAtUtc);
            existing.RecordConnectRequest(command.RequestId, command.RequestHash);
            await connectionRepository.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
            return Result.Success(ToModel(existing));
        }

        var connection = WearableConnection.Create(
            userId,
            provider,
            tokenResult.ExternalUserId,
            protectedAccessToken,
            protectedRefreshToken,
            tokenResult.ExpiresAtUtc);
        connection.RecordConnectRequest(command.RequestId, command.RequestHash);

        await connectionRepository.AddAsync(connection, cancellationToken).ConfigureAwait(false);
        return Result.Success(ToModel(connection));
    }

    private static WearableConnectionModel ToModel(WearableConnection c) =>
        new(c.Provider.ToString(),
            c.ExternalUserId,
            c.IsActive,
            c.LastSyncedAtUtc,
            c.CreatedOnUtc);
}
