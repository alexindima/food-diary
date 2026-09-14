using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Queries.GetUserBillingProfile;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Users.Queries.GetUserBillingProfile;

public sealed class GetUserBillingProfileQueryHandler(IUserLookupRepository userLookupRepository) : IRequestHandler<GetUserBillingProfileQuery, Result<UserBillingProfileModel>> {
    public async Task<Result<UserBillingProfileModel>> Handle(GetUserBillingProfileQuery request, CancellationToken cancellationToken) {
        UserId userId = request.UserId;
        User? user = await userLookupRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        Error? error = CurrentUserAccessPolicy.EnsureCanAccess(user);
        return error is null
            ? Result.Success(ToModel(user!))
            : Result.Failure<UserBillingProfileModel>(error);

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
