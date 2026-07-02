using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Tdee.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tdee.Services;

public sealed class TdeeUserProfileService(IUserRepository userRepository) : ITdeeUserProfileService {
    public async Task<Result<TdeeUserProfile>> GetAsync(UserId userId, CancellationToken cancellationToken = default) {
        User? user = await userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        Error? accessError = CurrentUserAccessPolicy.EnsureCanAccess(user);
        if (accessError is not null) {
            return Result.Failure<TdeeUserProfile>(accessError);
        }

        return Result.Success(new TdeeUserProfile(
            user!.CalculateBmr(),
            user.CalculateEstimatedTdee(),
            user.Weight,
            user.DesiredWeight,
            user.DailyCalorieTarget));
    }
}
