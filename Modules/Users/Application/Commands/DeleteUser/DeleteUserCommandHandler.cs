using FoodDiary.Application.Abstractions.Common.Abstractions.Audit;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Application.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Entities;

namespace FoodDiary.Modules.Users.Application.Commands.DeleteUser;

public sealed class DeleteUserCommandHandler(
    IUserContextService userContextService,
    TimeProvider dateTimeProvider,
    IUserSessionRevocationService refreshTokenSessionRepository,
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
