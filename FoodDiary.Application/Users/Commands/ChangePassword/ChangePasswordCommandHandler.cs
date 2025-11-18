using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using static FoodDiary.Application.Common.Abstractions.Result.Errors;

namespace FoodDiary.Application.Users.Commands.ChangePassword;

public class ChangePasswordCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher)
    : ICommandHandler<ChangePasswordCommand, Result<bool>> {
    public async Task<Result<bool>> Handle(ChangePasswordCommand command, CancellationToken cancellationToken) {
        var user = await userRepository.GetByIdAsync(command.UserId!.Value);
        if (user is null) {
            return Result.Failure<bool>(User.NotFound(command.UserId.Value));
        }

        var isCurrentPasswordValid = passwordHasher.Verify(command.CurrentPassword, user.Password);
        if (!isCurrentPasswordValid) {
            return Result.Failure<bool>(User.InvalidPassword);
        }

        var hashedPassword = passwordHasher.Hash(command.NewPassword);
        user.UpdatePassword(hashedPassword);

        await userRepository.UpdateAsync(user);

        return Result.Success(true);
    }
}