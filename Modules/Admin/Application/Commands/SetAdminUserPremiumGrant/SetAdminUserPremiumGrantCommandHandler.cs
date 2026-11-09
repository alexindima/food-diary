using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Application.Contracts.Common.Abstractions.Persistence;
using FoodDiary.Application.Contracts.Common.Abstractions.Results;
using FoodDiary.Audit.Contracts.Common.Abstractions.Audit;
using FoodDiary.Mediator;
using FoodDiary.Modules.Users.Contracts.Commands.SetUserManualPremiumGrant;
using FoodDiary.Modules.Users.Contracts.Common.Validation;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Commands.SetAdminUserPremiumGrant;

public sealed class SetAdminUserPremiumGrantCommandHandler(ISender users, IAuditLogger audit, IPostCommitActionQueue postCommitActions)
    : ICommandHandler<SetAdminUserPremiumGrantCommand, Result<UserManualPremiumGrantModel>> {
    public async Task<Result<UserManualPremiumGrantModel>> Handle(SetAdminUserPremiumGrantCommand command, CancellationToken cancellationToken) {
        Result<UserId> target = UserIdParser.Parse(command.UserId, Errors.Validation.Invalid("userId", "User id must not be empty."));
        if (target.IsFailure) {
            return UserIdParser.ToFailure<UserManualPremiumGrantModel>(target);
        }
        Result<UserId> actor = UserIdParser.Parse(command.ActorUserId, Errors.Validation.Invalid("actorUserId", "Actor user id must not be empty."));
        if (actor.IsFailure) {
            return UserIdParser.ToFailure<UserManualPremiumGrantModel>(actor);
        }

        Result<UserManualPremiumGrantModel> result = await users.Send(
            new SetUserManualPremiumGrantCommand(target.Value, command.EndsAtUtc, command.Reason), cancellationToken);
        if (result.IsSuccess) {
            string action = command.EndsAtUtc.HasValue ? "admin.user.premium_grant.set" : "admin.user.premium_grant.revoke";
            postCommitActions.Enqueue(action, token => {
                token.ThrowIfCancellationRequested();
                audit.Log(action, actor.Value, "User", command.UserId.ToString(), result.Value.EndsAtUtc?.ToString("O"));
                return Task.CompletedTask;
            });
        }
        return result;
    }
}
