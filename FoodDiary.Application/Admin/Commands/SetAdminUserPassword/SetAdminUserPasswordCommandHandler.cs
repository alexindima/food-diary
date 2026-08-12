using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Commands.SetAdminUserPassword;

public sealed class SetAdminUserPasswordCommandHandler(
    IUserAdministrationMutationService userManagementService,
    IRefreshTokenSessionWriteRepository refreshTokenSessionRepository,
    TimeProvider dateTimeProvider)
    : ICommandHandler<SetAdminUserPasswordCommand, Result> {
    public async Task<Result> Handle(SetAdminUserPasswordCommand command, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(
            command.UserId,
            Errors.Validation.Invalid(nameof(command.UserId), "User id must not be empty."));
        if (userIdResult.IsFailure) {
            return Result.Failure(userIdResult.Error);
        }

        Result passwordResult = await userManagementService
            .SetPasswordAsync(userIdResult.Value, command.NewPassword, cancellationToken)
            .ConfigureAwait(false);
        if (passwordResult.IsFailure) {
            return passwordResult;
        }

        await refreshTokenSessionRepository
            .RevokeAllAsync(userIdResult.Value, dateTimeProvider.GetUtcNow().UtcDateTime, cancellationToken)
            .ConfigureAwait(false);

        return Result.Success();
    }
}
