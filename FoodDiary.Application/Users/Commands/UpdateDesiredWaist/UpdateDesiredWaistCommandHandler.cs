using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Users.Commands.UpdateDesiredWaist;

public class UpdateDesiredWaistCommandHandler(IUserRepository userRepository)
    : ICommandHandler<UpdateDesiredWaistCommand, Result<UserDesiredWaistModel>> {
    public async Task<Result<UserDesiredWaistModel>> Handle(
        UpdateDesiredWaistCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<UserDesiredWaistModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null) {
            return Result.Failure<UserDesiredWaistModel>(Errors.User.NotFound(userId));
        }

        user.UpdateDesiredWaist(command.DesiredWaist);
        await userRepository.UpdateAsync(user, cancellationToken);

        return Result.Success(new UserDesiredWaistModel(user.DesiredWaist));
    }
}
