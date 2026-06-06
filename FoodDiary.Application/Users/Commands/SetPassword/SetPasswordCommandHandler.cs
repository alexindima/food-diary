using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using static FoodDiary.Application.Abstractions.Common.Abstractions.Result.Errors;
using FoodDiary.Application.Abstractions.Authentication.Common;

namespace FoodDiary.Application.Users.Commands.SetPassword;

public sealed class SetPasswordCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher)
    : ICommandHandler<SetPasswordCommand, Result> {
    public async Task<Result> Handle(SetPasswordCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId.Value);
        Domain.Entities.Users.User? user = await userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        Error? accessError = CurrentUserAccessPolicy.EnsureCanAccess(user);
        if (accessError is not null) {
            return Result.Failure(accessError);
        }

        Domain.Entities.Users.User currentUser = user!;
        if (currentUser.HasPassword) {
            return Result.Failure(User.PasswordAlreadySet);
        }

        string hashedPassword = passwordHasher.Hash(command.NewPassword);
        currentUser.UpdatePassword(hashedPassword);

        await userRepository.UpdateAsync(currentUser, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
