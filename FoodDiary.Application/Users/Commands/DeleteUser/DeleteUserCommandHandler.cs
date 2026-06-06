using FoodDiary.Application.Abstractions.Common.Abstractions.Audit;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Users.Commands.DeleteUser;

public class DeleteUserCommandHandler(
    IUserRepository userRepository,
    TimeProvider dateTimeProvider,
    IAuditLogger auditLogger)
    : ICommandHandler<DeleteUserCommand, Result> {
    public async Task<Result> Handle(DeleteUserCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);
        User? user = await userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        Error? accessError = CurrentUserAccessPolicy.EnsureCanAccess(user);
        if (accessError is not null) {
            return Result.Failure(accessError);
        }

        User currentUser = user!;

        currentUser.DeleteAccount(dateTimeProvider.GetUtcNow().UtcDateTime);
        await userRepository.UpdateAsync(currentUser, cancellationToken).ConfigureAwait(false);

        auditLogger.Log("user.delete", userId, "User", userId.Value.ToString());

        return Result.Success();
    }
}
