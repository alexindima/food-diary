using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Application.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Authentication.Common;

namespace FoodDiary.Modules.Users.Application.Commands.SetPassword;

public sealed class SetPasswordCommandHandler(
    IUserContextService userContextService,
    IPasswordHasher passwordHasher,
    IUserSessionRevocationService refreshTokenSessionRepository,
    TimeProvider dateTimeProvider)
    : ICommandHandler<SetPasswordCommand, Result> {
    public async Task<Result> Handle(SetPasswordCommand command, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            userContextService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return Result.Failure(userIdResult.Error);
        }

        UserId userId = userIdResult.Value;
        Result<FoodDiary.Modules.Users.Domain.Entities.User> userResult = await userContextService.GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure(userResult.Error);
        }

        FoodDiary.Modules.Users.Domain.Entities.User currentUser = userResult.Value;
        if (currentUser.HasPassword) {
            return Result.Failure(UserErrors.PasswordAlreadySet);
        }
        if (currentUser.Email is null || !currentUser.IsEmailConfirmed) {
            return Result.Failure(UserErrors.EmailRequired);
        }

        string hashedPassword = passwordHasher.Hash(command.NewPassword);
        currentUser.UpdatePassword(hashedPassword);

        await userContextService.UpdateUserAsync(currentUser, cancellationToken).ConfigureAwait(false);
        await refreshTokenSessionRepository
            .RevokeAllAsync(userId, dateTimeProvider.GetUtcNow().UtcDateTime, cancellationToken)
            .ConfigureAwait(false);

        return Result.Success();
    }
}
