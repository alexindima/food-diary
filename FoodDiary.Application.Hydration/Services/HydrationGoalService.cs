using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Hydration.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Hydration.Services;

public sealed class HydrationGoalService(IUserHydrationProfileReadService userProfileReadService) : IHydrationGoalService {
    public async Task<Result<double?>> GetCurrentGoalAsync(UserId userId, CancellationToken cancellationToken = default) {
        Result<UserHydrationProfileModel> profileResult = await userProfileReadService
            .GetHydrationProfileAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        if (profileResult.IsFailure) {
            return Result.Failure<double?>(profileResult.Error);
        }

        return Result.Success(profileResult.Value.EffectiveWaterGoal);
    }
}
