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
        if (command.UserId is null || command.UserId.Value == UserId.Empty) {
            return Result.Failure<UserDesiredWeightModel>(Errors.Authentication.InvalidToken);
        }

        var user = await userRepository.GetByIdAsync(command.UserId.Value);
        if (user is null) {
            return Result.Failure<UserDesiredWeightModel>(Errors.User.NotFound(command.UserId.Value));
        }

        user.UpdateDesiredWeight(command.DesiredWeight);
        await userRepository.UpdateAsync(user);

        return Result.Success(new UserDesiredWeightModel(user.DesiredWeight));
    }
}
