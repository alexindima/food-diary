using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Users.Commands.UpdateDesiredWeight;

public class UpdateDesiredWeightCommandHandler(IUserRepository userRepository)
    : ICommandHandler<UpdateDesiredWeightCommand, Result<UserDesiredWeightModel>> {
    public async Task<Result<UserDesiredWeightModel>> Handle(
        UpdateDesiredWeightCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<UserDesiredWeightModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);
        User? user = await userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        Error? accessError = CurrentUserAccessPolicy.EnsureCanAccess(user);
        if (accessError is not null) {
            return Result.Failure<UserDesiredWeightModel>(accessError);
        }

        User currentUser = user!;
        currentUser.UpdateDesiredWeight(command.DesiredWeight);
        await userRepository.UpdateAsync(currentUser, cancellationToken).ConfigureAwait(false);

        return Result.Success(new UserDesiredWeightModel(currentUser.DesiredWeight));
    }
}
