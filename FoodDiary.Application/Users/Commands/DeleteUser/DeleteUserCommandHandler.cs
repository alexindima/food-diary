using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Domain.ValueObjects.Ids;
using static FoodDiary.Application.Common.Abstractions.Result.Errors;

namespace FoodDiary.Application.Users.Commands.DeleteUser;

public class DeleteUserCommandHandler(
    IUserRepository userRepository,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<DeleteUserCommand, Result<bool>> {
    public async Task<Result<bool>> Handle(DeleteUserCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId.Value == Guid.Empty) {
            return Result.Failure<bool>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId.Value);
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null) {
            return Result.Failure<bool>(User.NotFound(userId));
        }

        user.UpdateRefreshToken(null);
        user.MarkDeleted(dateTimeProvider.UtcNow);
        await userRepository.UpdateAsync(user, cancellationToken);

        return Result.Success(true);
    }
}
