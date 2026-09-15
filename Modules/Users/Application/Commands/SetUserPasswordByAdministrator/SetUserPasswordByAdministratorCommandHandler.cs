using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Commands.SetUserPasswordByAdministrator;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Users.Commands.SetUserPasswordByAdministrator;

public sealed class SetUserPasswordByAdministratorCommandHandler(IUserLookupRepository userLookupRepository,
    IUserWriteRepository userWriteRepository,
    IPasswordHasher passwordHasher) : IRequestHandler<SetUserPasswordByAdministratorCommand, Result> {
    public async Task<Result> Handle(SetUserPasswordByAdministratorCommand request, CancellationToken cancellationToken) {
        FoodDiary.Domain.ValueObjects.Ids.UserId userId = request.UserId;
        FoodDiary.Domain.ValueObjects.Ids.UserId actorUserId = request.ActorUserId;
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
