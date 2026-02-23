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
        if (command.UserId is null || command.UserId.Value == UserId.Empty) {
            return Result.Failure<bool>(Errors.Authentication.InvalidToken);
        }

        var user = await userRepository.GetByIdAsync(command.UserId.Value);
        if (user is null) {
            return Result.Failure<bool>(User.NotFound(command.UserId.Value));
        }

        user.MarkDeleted(dateTimeProvider.UtcNow);
        user.UpdateRefreshToken(null);
        await userRepository.UpdateAsync(user);

        return Result.Success(true);
    }
}
