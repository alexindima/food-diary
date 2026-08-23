using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Audit;

namespace FoodDiary.Application.Admin.Commands.SetAdminUserPassword;

public sealed class SetAdminUserPasswordCommandHandler(
    IUserAdministrationMutationService userManagementService,
    IRefreshTokenSessionWriteRepository refreshTokenSessionRepository,
    TimeProvider dateTimeProvider,
    IAuditLogger auditLogger)
    : ICommandHandler<SetAdminUserPasswordCommand, Result> {
    public async Task<Result> Handle(SetAdminUserPasswordCommand command, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(
            command.UserId,
            Errors.Validation.Invalid(nameof(command.UserId), "User id must not be empty."));
        if (userIdResult.IsFailure) {
            return Result.Failure(userIdResult.Error);
        }

        Result<UserId> actorUserIdResult = UserIdParser.Parse(
            command.ActorUserId,
            Errors.Validation.Invalid(nameof(command.ActorUserId), "Actor user id must not be empty."));
        if (actorUserIdResult.IsFailure) {
            return Result.Failure(actorUserIdResult.Error);
        }

        Result passwordResult = await userManagementService
            .SetPasswordAsync(userIdResult.Value, actorUserIdResult.Value, command.NewPassword, cancellationToken)
            .ConfigureAwait(false);
        if (passwordResult.IsFailure) {
            return passwordResult;
        }

        await refreshTokenSessionRepository
            .RevokeAllAsync(userIdResult.Value, dateTimeProvider.GetUtcNow().UtcDateTime, cancellationToken)
            .ConfigureAwait(false);

        auditLogger.Log(
            "admin.user.password-reset",
            actorUserIdResult.Value,
            "User",
            userIdResult.Value.Value.ToString());

        return Result.Success();
    }
}
