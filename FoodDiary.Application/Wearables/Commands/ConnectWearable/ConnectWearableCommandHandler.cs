using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Wearables.Common;
using FoodDiary.Application.Abstractions.Wearables.Models;
using FoodDiary.Domain.Entities.Wearables;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Wearables.Commands.ConnectWearable;

public class ConnectWearableCommandHandler(
    IEnumerable<IWearableClient> wearableClients,
    IWearableConnectionRepository connectionRepository)
    : ICommandHandler<ConnectWearableCommand, Result<WearableConnectionModel>> {
    public async Task<Result<WearableConnectionModel>> Handle(
        ConnectWearableCommand command,
        CancellationToken cancellationToken) {
        var userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<WearableConnectionModel>(userIdResult.Error);
        }

        if (!Enum.TryParse<WearableProvider>(command.Provider, true, out var provider)) {
            return Result.Failure<WearableConnectionModel>(Errors.Wearable.InvalidProvider(command.Provider));
        }

        var client = wearableClients.FirstOrDefault(c => c.Provider == provider);
        if (client is null) {
            return Result.Failure<WearableConnectionModel>(Errors.Wearable.ProviderNotConfigured(command.Provider));
        }

        var tokenResult = await client.ExchangeCodeAsync(command.Code, cancellationToken);
        if (tokenResult is null) {
            return Result.Failure<WearableConnectionModel>(Errors.Wearable.AuthFailed(command.Provider));
        }

        var existing = await connectionRepository.GetAsync(userIdResult.Value, provider, cancellationToken);
        if (existing is not null) {
            existing.UpdateTokens(tokenResult.AccessToken, tokenResult.RefreshToken, tokenResult.ExpiresAtUtc);
            if (!existing.IsActive) {
                existing = WearableConnection.Create(
                    userIdResult.Value,
                    provider,
                    tokenResult.ExternalUserId,
                    tokenResult.AccessToken,
                    tokenResult.RefreshToken,
                    tokenResult.ExpiresAtUtc);
            }
            await connectionRepository.UpdateAsync(existing, cancellationToken);
            return Result.Success(ToModel(existing));
        }

        var connection = WearableConnection.Create(
            userIdResult.Value,
            provider,
            tokenResult.ExternalUserId,
            tokenResult.AccessToken,
            tokenResult.RefreshToken,
            tokenResult.ExpiresAtUtc);

        await connectionRepository.AddAsync(connection, cancellationToken);
        return Result.Success(ToModel(connection));
    }

    private static WearableConnectionModel ToModel(WearableConnection c) =>
        new(c.Provider.ToString(),
            c.ExternalUserId,
            c.IsActive,
            c.LastSyncedAtUtc,
            c.CreatedOnUtc);
}
