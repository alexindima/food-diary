using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using static FoodDiary.Application.Abstractions.Common.Abstractions.Result.Errors;
using FoodDiary.Application.Abstractions.Authentication.Common;

namespace FoodDiary.Application.Users.Commands.ChangePassword;

public class ChangePasswordCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher)
    : ICommandHandler<ChangePasswordCommand, Result> {
    public async Task<Result> Handle(ChangePasswordCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        var accessError = CurrentUserAccessPolicy.EnsureCanAccess(user);
        if (accessError is not null) {
            return Result.Failure(accessError);
        }

        var currentUser = user!;
        if (!currentUser.HasPassword) {
            return Result.Failure(User.PasswordNotSet);
        }

        var isCurrentPasswordValid = passwordHasher.Verify(command.CurrentPassword, currentUser.Password);
        if (!isCurrentPasswordValid) {
            return Result.Failure(User.InvalidPassword);
        }

        var hashedPassword = passwordHasher.Hash(command.NewPassword);
        currentUser.UpdatePassword(hashedPassword);

        await userRepository.UpdateAsync(currentUser, cancellationToken);

        return Result.Success();
    }
}
