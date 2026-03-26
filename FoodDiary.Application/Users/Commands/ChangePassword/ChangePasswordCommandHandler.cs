using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Domain.ValueObjects.Ids;
using static FoodDiary.Application.Common.Abstractions.Result.Errors;

namespace FoodDiary.Application.Users.Commands.ChangePassword;

public class ChangePasswordCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher)
    : ICommandHandler<ChangePasswordCommand, Result<bool>> {
    public async Task<Result<bool>> Handle(ChangePasswordCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId.Value == Guid.Empty) {
            return Result.Failure<bool>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId.Value);
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null) {
            return Result.Failure<bool>(User.NotFound(userId));
        }

        var isCurrentPasswordValid = passwordHasher.Verify(command.CurrentPassword, user.Password);
        if (!isCurrentPasswordValid) {
            return Result.Failure<bool>(User.InvalidPassword);
        }

        var hashedPassword = passwordHasher.Hash(command.NewPassword);
        user.UpdatePassword(hashedPassword);

        await userRepository.UpdateAsync(user, cancellationToken);

        return Result.Success(true);
    }
}
