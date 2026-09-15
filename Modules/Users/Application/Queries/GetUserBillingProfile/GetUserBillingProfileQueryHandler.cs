using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Abstractions.Users.Queries.GetUserBillingProfile;
using FoodDiary.Mediator;
using FoodDiary.Results;

namespace FoodDiary.Application.Users.Queries.GetUserBillingProfile;

public sealed class GetUserBillingProfileQueryHandler(IUserBillingProfileReadModelRepository repository) : IRequestHandler<GetUserBillingProfileQuery, Result<UserBillingProfileModel>> {
    public async Task<Result<UserBillingProfileModel>> Handle(GetUserBillingProfileQuery request, CancellationToken cancellationToken) {
        UserBillingProfileModel? profile = await repository.GetBillingProfileIncludingDeletedAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        // Match the persisted access predicate of the former tracked lookup.
        return profile is { IsActive: true, IsDeleted: false }
            ? Result.Success(profile)
            : Result.Failure<UserBillingProfileModel>(AuthenticationErrors.InvalidToken);
    }
}
