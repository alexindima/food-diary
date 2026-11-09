using FoodDiary.Authentication.Contracts.Authentication.Common;
using FoodDiary.Modules.Users.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Contracts.Queries.GetUserBillingProfile;
using FoodDiary.Mediator;
using FoodDiary.Results;

namespace FoodDiary.Modules.Users.Application.Queries.GetUserBillingProfile;

public sealed class GetUserBillingProfileQueryHandler(IUserBillingProfileReadModelRepository repository, IUserPremiumTrialStore trials)
    : IRequestHandler<GetUserBillingProfileQuery, Result<UserBillingProfileModel>> {
    public async Task<Result<UserBillingProfileModel>> Handle(GetUserBillingProfileQuery request, CancellationToken cancellationToken) {
        UserBillingProfileModel? profile = await repository.GetBillingProfileIncludingDeletedAsync(request.UserId, cancellationToken);
        // Match the persisted access predicate of the former tracked lookup.
        if (profile is not { IsActive: true, IsDeleted: false }) {
            return Result.Failure<UserBillingProfileModel>(AuthenticationErrors.InvalidToken);
        }
        if (!profile.HasUsedPremiumTrial && profile is { HasVerifiedTelegramLink: true, TelegramUserId: { } telegramId } &&
            await trials.HasRedeemedTelegramAsync(telegramId, cancellationToken)) {
            profile = profile with { PremiumTrialRestrictedByTelegram = true };
        }
        return Result.Success(profile);
    }
}
