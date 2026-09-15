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
    ICurrentUserAccessService currentUserAccessService)
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

        WearableConnection? connection = await connectionRepository.GetAsync(userIdResult.Value, provider, cancellationToken).ConfigureAwait(false);
        if (connection is null) {
            return Result.Failure(WearableErrors.NotConnected(command.Provider));
        }

        connection.Deactivate();
        await connectionRepository.UpdateAsync(connection, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
