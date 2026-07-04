using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Users.Commands.UpdateDesiredWaist;

public sealed class UpdateDesiredWaistCommandHandler(IUserContextService userContextService)
    : ICommandHandler<UpdateDesiredWaistCommand, Result<UserDesiredWaistModel>> {
    public async Task<Result<UserDesiredWaistModel>> Handle(
        UpdateDesiredWaistCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<UserDesiredWaistModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);
        Result<User> userResult = await userContextService.GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure<UserDesiredWaistModel>(userResult.Error);
        }

        User currentUser = userResult.Value;
        currentUser.UpdateDesiredWaist(command.DesiredWaist);
        await userContextService.UpdateUserAsync(currentUser, cancellationToken).ConfigureAwait(false);

        return Result.Success(new UserDesiredWaistModel(currentUser.DesiredWaist));
    }
}
