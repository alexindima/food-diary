using FoodDiary.Application.Abstractions.Common.Abstractions.Audit;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Application.Abstractions.Authentication.Common;

namespace FoodDiary.Application.Users.Commands.DeleteUser;

public sealed class DeleteUserCommandHandler(
    IUserContextService userContextService,
    TimeProvider dateTimeProvider,
    IRefreshTokenSessionWriteRepository refreshTokenSessionRepository,
    IAuditLogger auditLogger)
    : ICommandHandler<DeleteUserCommand, Result> {
    public async Task<Result> Handle(DeleteUserCommand command, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            userContextService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return Result.Failure(userIdResult.Error);
        }

        UserId userId = userIdResult.Value;
        Result<User> userResult = await userContextService.GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure(userResult.Error);
        }

        User currentUser = userResult.Value;

        DateTime deletedAtUtc = dateTimeProvider.GetUtcNow().UtcDateTime;
        currentUser.DeleteAccount(deletedAtUtc);
        await userContextService.UpdateUserAsync(currentUser, cancellationToken).ConfigureAwait(false);
        await refreshTokenSessionRepository
            .RevokeAllAsync(userId, deletedAtUtc, cancellationToken)
            .ConfigureAwait(false);

        auditLogger.Log("user.delete", userId, "User", userId.Value.ToString());

        return Result.Success();
    }
}
