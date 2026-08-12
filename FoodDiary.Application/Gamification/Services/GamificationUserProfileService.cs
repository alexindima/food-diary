using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Gamification.Common;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Gamification.Services;

public sealed class GamificationUserProfileService(IUserGamificationProfileReadService userProfileReadService) : IGamificationUserProfileService {
    public async Task<Result<IGamificationUserProfile>> GetAsync(UserId userId, CancellationToken cancellationToken = default) {
        Result<UserGamificationProfileModel> profileResult = await userProfileReadService
            .GetGamificationProfileAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        if (profileResult.IsFailure) {
            return Result.Failure<IGamificationUserProfile>(profileResult.Error);
        }

        return Result.Success<IGamificationUserProfile>(new GamificationUserProfile(profileResult.Value.CalorieSchedule));
    }

    private sealed class GamificationUserProfile(UserCalorieSchedule calorieSchedule) : IGamificationUserProfile {
        public double? GetCalorieTargetForDate(DateTime date) => calorieSchedule.GetTargetForDate(date);
    }
}
