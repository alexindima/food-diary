using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Wearables.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Wearables;

namespace FoodDiary.Application.Wearables.Commands.DisconnectWearable;

public class DisconnectWearableCommandHandler(IWearableConnectionRepository connectionRepository)
    : ICommandHandler<DisconnectWearableCommand, Result> {
    public async Task<Result> Handle(
        DisconnectWearableCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure(userIdResult.Error);
        }

        if (!Enum.TryParse<WearableProvider>(command.Provider, true, out WearableProvider provider)) {
            return Result.Failure(Errors.Wearable.InvalidProvider(command.Provider));
        }

        WearableConnection? connection = await connectionRepository.GetAsync(userIdResult.Value, provider, cancellationToken).ConfigureAwait(false);
        if (connection is null) {
            return Result.Failure(Errors.Wearable.NotConnected(command.Provider));
        }

        connection.Deactivate();
        await connectionRepository.UpdateAsync(connection, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
