using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Wearables.Common;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Wearables.Commands.DisconnectWearable;

public class DisconnectWearableCommandHandler(IWearableConnectionRepository connectionRepository)
    : ICommandHandler<DisconnectWearableCommand, Result> {
    public async Task<Result> Handle(
        DisconnectWearableCommand command,
        CancellationToken cancellationToken) {
        var userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure(userIdResult.Error);
        }

        if (!Enum.TryParse<WearableProvider>(command.Provider, true, out var provider)) {
            return Result.Failure(Errors.Wearable.InvalidProvider(command.Provider));
        }

        var connection = await connectionRepository.GetAsync(userIdResult.Value, provider, cancellationToken);
        if (connection is null) {
            return Result.Failure(Errors.Wearable.NotConnected(command.Provider));
        }

        connection.Deactivate();
        await connectionRepository.UpdateAsync(connection, cancellationToken);
        return Result.Success();
    }
}
