using FoodDiary.Results;
using FoodDiary.Application.Tdee.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tdee.Services;

public sealed class TdeeUserProfileService(IUserContextService userContextService) : ITdeeUserProfileService {
    public async Task<Result<TdeeUserProfile>> GetAsync(UserId userId, CancellationToken cancellationToken = default) {
        Result<User> userResult = await userContextService.GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure<TdeeUserProfile>(userResult.Error);
        }

        User user = userResult.Value;
        return Result.Success(new TdeeUserProfile(
            user.CalculateBmr(),
            user.CalculateEstimatedTdee(),
            user.Weight,
            user.DesiredWeight,
            user.DailyCalorieTarget));
    }
}
