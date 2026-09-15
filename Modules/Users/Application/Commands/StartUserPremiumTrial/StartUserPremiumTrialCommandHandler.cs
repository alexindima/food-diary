using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Commands.StartUserPremiumTrial;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Users.Commands.StartUserPremiumTrial;

public sealed class StartUserPremiumTrialCommandHandler(IUserLookupRepository userLookupRepository,
    IUserWriteRepository userWriteRepository) : IRequestHandler<StartUserPremiumTrialCommand, Result<UserBillingProfileModel>> {
    public async Task<Result<UserBillingProfileModel>> Handle(StartUserPremiumTrialCommand request, CancellationToken cancellationToken) {
        UserId userId = request.UserId;
        DateTime startedAtUtc = request.StartedAtUtc;
        TimeSpan duration = request.Duration;
        User? user = await userLookupRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        Error? error = CurrentUserAccessPolicy.EnsureCanAccess(user);
        if (error is not null) {
            return Result.Failure<UserBillingProfileModel>(error);
        }

        user!.StartPremiumTrial(startedAtUtc, duration);
        await userWriteRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        return Result.Success(ToModel(user));

    }

    private static UserBillingProfileModel ToModel(User user) =>
        new(
            user.Id,
            user.Email,
            user.IsActive,
            user.DeletedAt is not null,
            user.HasRole(RoleNames.Premium),
            user.PremiumTrialStartedAtUtc,
            user.PremiumTrialEndsAtUtc,
            user.IsEmailConfirmed);

}
