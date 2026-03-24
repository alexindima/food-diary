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
        if (command.UserId is null || command.UserId.Value == UserId.Empty) {
            return Result.Failure<UserDesiredWaistModel>(Errors.Authentication.InvalidToken);
        }

        var user = await userRepository.GetByIdAsync(command.UserId.Value);
        if (user is null) {
            return Result.Failure<UserDesiredWaistModel>(Errors.User.NotFound(command.UserId.Value));
        }

        user.UpdateDesiredWaist(command.DesiredWaist);
        await userRepository.UpdateAsync(user);

        return Result.Success(new UserDesiredWaistModel(user.DesiredWaist));
    }
}
