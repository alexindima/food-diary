using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.WeeklyCheckIn.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.WeeklyCheckIn.Services;

public sealed class WeeklyCheckInUserProfileService(IUserContextService userContextService) : IWeeklyCheckInUserProfileService {
    public async Task<Result<WeeklyCheckInUserProfile>> GetAsync(UserId userId, CancellationToken cancellationToken = default) {
        Result<User> userResult = await userContextService.GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure<WeeklyCheckInUserProfile>(userResult.Error);
        }

        return Result.Success(new WeeklyCheckInUserProfile(userResult.Value.DailyCalorieTarget));
    }
}
