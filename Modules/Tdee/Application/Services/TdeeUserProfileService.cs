using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Tdee.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tdee.Services;

public sealed class TdeeUserProfileService(IUserTdeeProfileReadService userProfileReadService) : ITdeeUserProfileService {
    public async Task<Result<TdeeUserProfile>> GetAsync(UserId userId, CancellationToken cancellationToken = default) {
        Result<UserTdeeProfileModel> profileResult = await userProfileReadService
            .GetTdeeProfileAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        if (profileResult.IsFailure) {
            return Result.Failure<TdeeUserProfile>(profileResult.Error);
        }

        UserTdeeProfileModel profile = profileResult.Value;
        return Result.Success(new TdeeUserProfile(
            profile.Bmr,
            profile.EstimatedTdee,
            profile.WeightKg,
            profile.DesiredWeightKg,
            profile.DailyCalorieTarget));
    }
}
