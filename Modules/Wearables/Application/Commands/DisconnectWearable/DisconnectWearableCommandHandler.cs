using FoodDiary.Modules.Users.Contracts.Common.Validation;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Wearables.Application.Abstractions.Common;
using FoodDiary.Modules.Wearables.Application.Common;
using FoodDiary.Modules.Wearables.Domain.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Wearables.Domain.Entities;

namespace FoodDiary.Modules.Wearables.Application.Commands.DisconnectWearable;

public sealed class DisconnectWearableCommandHandler(
    IWearableConnectionWriteRepository connectionRepository,
    ICurrentUserAccessService currentUserAccessService,
    IWearableTransactionRunner transactionRunner)
    : ICommandHandler<DisconnectWearableCommand, Result> {
    public async Task<Result> Handle(
        DisconnectWearableCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure(userIdResult);
        }

        Result<WearableProvider> providerResult = WearableProviderParser.Parse(command.Provider);
        if (providerResult.IsFailure) {
            return Result.Failure(providerResult.Error);
        }

        WearableProvider provider = providerResult.Value;

        return await transactionRunner.ExecuteSerializedAsync(
            WearableConnectionLock.Key(userIdResult.Value, provider),
            token => DisconnectAsync(userIdResult.Value, provider, token),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<Result> DisconnectAsync(UserId userId, WearableProvider provider, CancellationToken cancellationToken) {
        WearableConnection? connection = await connectionRepository.GetAsync(userId, provider, cancellationToken).ConfigureAwait(false);
        if (connection is null) {
            return Result.Failure(WearableErrors.NotConnected(provider.ToString()));
        }

        connection.Deactivate();
        await connectionRepository.UpdateAsync(connection, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
