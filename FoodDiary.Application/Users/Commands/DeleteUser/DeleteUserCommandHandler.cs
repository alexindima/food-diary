using FoodDiary.Application.Common.Abstractions.Audit;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Domain.ValueObjects.Ids;
using static FoodDiary.Application.Common.Abstractions.Result.Errors;

namespace FoodDiary.Application.Users.Commands.DeleteUser;

public class DeleteUserCommandHandler(
    IUserRepository userRepository,
    IDateTimeProvider dateTimeProvider,
    IAuditLogger auditLogger)
    : ICommandHandler<DeleteUserCommand, Result> {
    public async Task<Result> Handle(DeleteUserCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null) {
            return Result.Failure(User.NotFound(userId));
        }

        user.DeleteAccount(dateTimeProvider.UtcNow);
        await userRepository.UpdateAsync(user, cancellationToken);

        auditLogger.Log("user.delete", userId, "User", userId.Value.ToString());

        return Result.Success();
    }
}
