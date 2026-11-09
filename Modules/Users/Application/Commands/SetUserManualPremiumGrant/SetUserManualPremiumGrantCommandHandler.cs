using FoodDiary.Application.Contracts.Common.Abstractions.Results;
using FoodDiary.Mediator;
using FoodDiary.Modules.Users.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Contracts.Commands.SetUserManualPremiumGrant;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Results;

namespace FoodDiary.Modules.Users.Application.Commands.SetUserManualPremiumGrant;

public sealed class SetUserManualPremiumGrantCommandHandler(IUserRepository users, TimeProvider clock)
    : IRequestHandler<SetUserManualPremiumGrantCommand, Result<UserManualPremiumGrantModel>> {
    public async Task<Result<UserManualPremiumGrantModel>> Handle(SetUserManualPremiumGrantCommand request, CancellationToken cancellationToken) {
        DateTime? end = request.EndsAt?.UtcDateTime;
        string? reason = request.Reason?.Trim();
        if (end <= clock.GetUtcNow().UtcDateTime) {
            return Result.Failure<UserManualPremiumGrantModel>(Errors.Validation.Invalid("endsAtUtc", "Premium must end in the future."));
        }
        if (end.HasValue && (string.IsNullOrEmpty(reason) || reason.Length > User.ManualPremiumReasonMaxLength)) {
            return Result.Failure<UserManualPremiumGrantModel>(Errors.Validation.Invalid("reason", "A reason of up to 500 characters is required."));
        }
        if (request.UserId == SystemContentOwner.Id) {
            return Result.Failure<UserManualPremiumGrantModel>(UserErrors.NotFound(request.UserId));
        }
        User? user = await users.GetByIdIncludingDeletedAsync(request.UserId, cancellationToken);
        if (user is null || user.DeletedAt is not null) {
            return Result.Failure<UserManualPremiumGrantModel>(UserErrors.NotFound(request.UserId));
        }
        user.SetManualPremiumGrant(end, reason);
        await users.UpdateAsync(user, cancellationToken);
        return Result.Success(new UserManualPremiumGrantModel(user.ManualPremiumEndsAtUtc, user.ManualPremiumReason));
    }
}
