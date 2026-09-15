using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Users.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Commands.SetUserPasswordByAdministrator;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Users.Application.Commands.SetUserPasswordByAdministrator;

public sealed class SetUserPasswordByAdministratorCommandHandler(IUserLookupRepository userLookupRepository,
    IUserWriteRepository userWriteRepository,
    IPasswordHasher passwordHasher) : IRequestHandler<SetUserPasswordByAdministratorCommand, Result> {
    public async Task<Result> Handle(SetUserPasswordByAdministratorCommand request, CancellationToken cancellationToken) {
        FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids.UserId userId = request.UserId;
        FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids.UserId actorUserId = request.ActorUserId;
        string newPassword = request.NewPassword;
        User? user = await userLookupRepository
            .GetByIdIncludingDeletedAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        if (user is null) {
            return Result.Failure(UserErrors.NotFound(userId));
        }

        if (userId == actorUserId || user.HasRole(RoleNames.Owner) || user.HasRole(RoleNames.Admin)) {
            return Result.Failure(UserErrors.AdminPasswordResetForbidden);
        }

        user.UpdatePassword(passwordHasher.Hash(newPassword));
        user.RequirePasswordChange();
        await userWriteRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        return Result.Success();

    }

}
