using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Gamification.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Gamification.Services;

public sealed class GamificationUserProfileService(IUserContextService userContextService) : IGamificationUserProfileService {
    public async Task<Result<IGamificationUserProfile>> GetAsync(UserId userId, CancellationToken cancellationToken = default) {
        Result<User> userResult = await userContextService.GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure<IGamificationUserProfile>(userResult.Error);
        }

        return Result.Success<IGamificationUserProfile>(new GamificationUserProfile(userResult.Value));
    }

    private sealed class GamificationUserProfile(User user) : IGamificationUserProfile {
        public double? GetCalorieTargetForDate(DateTime date) => user.GetCalorieTargetForDate(date);
    }
}
