using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Users.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Contracts.Queries.GetUserBillingProfile;
using FoodDiary.Mediator;
using FoodDiary.Results;

namespace FoodDiary.Modules.Users.Application.Queries.GetUserBillingProfile;

public sealed class GetUserBillingProfileQueryHandler(IUserBillingProfileReadModelRepository repository) : IRequestHandler<GetUserBillingProfileQuery, Result<UserBillingProfileModel>> {
    public async Task<Result<UserBillingProfileModel>> Handle(GetUserBillingProfileQuery request, CancellationToken cancellationToken) {
        UserBillingProfileModel? profile = await repository.GetBillingProfileIncludingDeletedAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        // Match the persisted access predicate of the former tracked lookup.
        return profile is { IsActive: true, IsDeleted: false }
            ? Result.Success(profile)
            : Result.Failure<UserBillingProfileModel>(AuthenticationErrors.InvalidToken);
    }
}
