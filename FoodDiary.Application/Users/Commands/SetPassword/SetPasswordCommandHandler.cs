using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using static FoodDiary.Application.Abstractions.Common.Abstractions.Results.Errors;
using FoodDiary.Application.Abstractions.Authentication.Common;

namespace FoodDiary.Application.Users.Commands.SetPassword;

public sealed class SetPasswordCommandHandler(
    IUserContextService userContextService,
    IPasswordHasher passwordHasher)
    : ICommandHandler<SetPasswordCommand, Result> {
    public async Task<Result> Handle(SetPasswordCommand command, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure(userIdResult);
        }

        UserId userId = userIdResult.Value;
        Result<Domain.Entities.Users.User> userResult = await userContextService.GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure(userResult.Error);
        }

        Domain.Entities.Users.User currentUser = userResult.Value;
        if (currentUser.HasPassword) {
            return Result.Failure(User.PasswordAlreadySet);
        }

        string hashedPassword = passwordHasher.Hash(command.NewPassword);
        currentUser.UpdatePassword(hashedPassword);

        await userContextService.UpdateUserAsync(currentUser, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
