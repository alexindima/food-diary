using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Gamification.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Gamification.Services;

public sealed class GamificationUserProfileService(IUserRepository userRepository) : IGamificationUserProfileService {
    public async Task<Result<IGamificationUserProfile>> GetAsync(UserId userId, CancellationToken cancellationToken = default) {
        User? user = await userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        Error? accessError = CurrentUserAccessPolicy.EnsureCanAccess(user);
        if (accessError is not null) {
            return Result.Failure<IGamificationUserProfile>(accessError);
        }

        return Result.Success<IGamificationUserProfile>(new GamificationUserProfile(user!));
    }

    private sealed class GamificationUserProfile(User user) : IGamificationUserProfile {
        public double? GetCalorieTargetForDate(DateTime date) => user.GetCalorieTargetForDate(date);
    }
}
