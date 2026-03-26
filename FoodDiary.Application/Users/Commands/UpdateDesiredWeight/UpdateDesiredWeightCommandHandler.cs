using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Users.Commands.UpdateDesiredWeight;

public class UpdateDesiredWeightCommandHandler(IUserRepository userRepository)
    : ICommandHandler<UpdateDesiredWeightCommand, Result<UserDesiredWeightModel>> {
    public async Task<Result<UserDesiredWeightModel>> Handle(
        UpdateDesiredWeightCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId.Value == Guid.Empty) {
            return Result.Failure<UserDesiredWeightModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId.Value);
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null) {
            return Result.Failure<UserDesiredWeightModel>(Errors.User.NotFound(userId));
        }

        user.UpdateDesiredWeight(command.DesiredWeight);
        await userRepository.UpdateAsync(user, cancellationToken);

        return Result.Success(new UserDesiredWeightModel(user.DesiredWeight));
    }
}
