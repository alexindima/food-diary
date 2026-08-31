using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Abstractions.Common.Abstractions.Audit;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Admin.Mappings;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Admin.Internal.Validation;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Commands.UpdateAdminUser;

public sealed class UpdateAdminUserCommandHandler(
    IUserAdministrationMutationService userManagementService,
    IAuditLogger auditLogger,
    TimeProvider dateTimeProvider)
    : ICommandHandler<UpdateAdminUserCommand, Result<AdminUserModel>> {
    public async Task<Result<AdminUserModel>> Handle(
        UpdateAdminUserCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(
            command.UserId,
            Errors.Validation.Invalid(nameof(command.UserId), "User id must not be empty."));
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<AdminUserModel>(userIdResult);
        }

        Result<UserId?> actorUserIdResult = OptionalEntityIdValidator.Parse(
            command.ActorUserId,
            nameof(command.ActorUserId),
            "Actor user id",
            value => new UserId(value));
        if (actorUserIdResult.IsFailure) {
            return Result.Failure<AdminUserModel>(actorUserIdResult.Error);
        }

        Result<UserAdminReadModel> updateResult = await userManagementService
            .UpdateAsync(
                new UserAdminUpdateModel(
                    userIdResult.Value,
                    command.IsActive,
                    command.IsEmailConfirmed,
                    command.Roles,
                    command.Language,
                    command.AiInputTokenLimit,
                    command.AiOutputTokenLimit,
                    actorUserIdResult.Value,
                    dateTimeProvider.GetUtcNow().UtcDateTime),
                cancellationToken)
            .ConfigureAwait(false);
        if (updateResult.IsFailure) {
            return Result.Failure<AdminUserModel>(updateResult.Error);
        }

        auditLogger.Log(
            "admin.user.update",
            userIdResult.Value,
            "User",
            command.UserId.ToString(),
            $"roles={command.Roles?.Count.ToString() ?? "unchanged"} isActive={command.IsActive?.ToString() ?? "unchanged"}");

        return Result.Success(updateResult.Value.ToAdminModel());
    }
}
