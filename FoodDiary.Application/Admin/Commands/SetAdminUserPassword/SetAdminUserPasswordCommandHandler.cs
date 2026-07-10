using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Commands.SetAdminUserPassword;

public sealed class SetAdminUserPasswordCommandHandler(
    IAdminUserManagementService userManagementService,
    IPasswordHasher passwordHasher)
    : ICommandHandler<SetAdminUserPasswordCommand, Result> {
    public async Task<Result> Handle(SetAdminUserPasswordCommand command, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(
            command.UserId,
            Errors.Validation.Invalid(nameof(command.UserId), "User id must not be empty."));
        if (userIdResult.IsFailure) {
            return Result.Failure(userIdResult.Error);
        }

        Domain.Entities.Users.User? user = await userManagementService
            .GetByIdIncludingDeletedAsync(userIdResult.Value, cancellationToken)
            .ConfigureAwait(false);
        if (user is null) {
            return Result.Failure(Errors.User.NotFound(command.UserId));
        }

        user.UpdatePassword(passwordHasher.Hash(command.NewPassword));

        await userManagementService.UpdateAsync(user, [], cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
