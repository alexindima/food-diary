using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Wearables.Common;
using FoodDiary.Application.Abstractions.Wearables.Models;
using FoodDiary.Application.Wearables.Wearables.Common;
using FoodDiary.Domain.Entities.Wearables;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Wearables.Wearables.Commands.ConnectWearable;

public sealed class ConnectWearableCommandHandler(
    IEnumerable<IWearableClient> wearableClients,
    IWearableConnectionWriteRepository connectionRepository,
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

        WearableConnection? existing = await connectionRepository
            .GetAsync(userIdResult.Value, provider, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null && string.Equals(existing.LastConnectRequestId, command.RequestId, StringComparison.Ordinal)) {
            return string.Equals(existing.LastConnectRequestHash, command.RequestHash, StringComparison.Ordinal)
                ? Result.Success(ToModel(existing))
                : Result.Failure<WearableConnectionModel>(Errors.Idempotency.Conflict);
        }

        IWearableClient? client = wearableClients.FirstOrDefault(c => c.Provider == provider);
        if (client is null) {
            return Result.Failure<WearableConnectionModel>(Errors.Wearable.ProviderNotConfigured(command.Provider));
        }

        if (!stateService.IsValidState(command.State, userIdResult.Value, provider)) {
            return Result.Failure<WearableConnectionModel>(Errors.Wearable.InvalidState);
        }

        WearableTokenResult? tokenResult = await client.ExchangeCodeAsync(command.Code, cancellationToken).ConfigureAwait(false);
        if (tokenResult is null) {
            return Result.Failure<WearableConnectionModel>(Errors.Wearable.AuthFailed(command.Provider));
        }

        string protectedAccessToken = tokenProtector.Protect(tokenResult.AccessToken);
        string? protectedRefreshToken = tokenResult.RefreshToken is null ? null : tokenProtector.Protect(tokenResult.RefreshToken);
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
            userIdResult.Value,
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
