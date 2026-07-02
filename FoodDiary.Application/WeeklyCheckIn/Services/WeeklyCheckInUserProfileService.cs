using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.WeeklyCheckIn.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.WeeklyCheckIn.Services;

public sealed class WeeklyCheckInUserProfileService(IUserRepository userRepository) : IWeeklyCheckInUserProfileService {
    public async Task<Result<WeeklyCheckInUserProfile>> GetAsync(UserId userId, CancellationToken cancellationToken = default) {
        User? user = await userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        Error? accessError = CurrentUserAccessPolicy.EnsureCanAccess(user);
        if (accessError is not null) {
            return Result.Failure<WeeklyCheckInUserProfile>(accessError);
        }

        return Result.Success(new WeeklyCheckInUserProfile(user!.DailyCalorieTarget));
    }
}
