using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.WeeklyCheckIn.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.WeeklyCheckIn.Services;

public sealed class WeeklyCheckInUserProfileService(
    ICurrentUserAccessService currentUserAccessService,
    IUserWeeklyCheckInProfileReadService userProfileReadService) : IWeeklyCheckInUserProfileService {
    public Task<Error?> EnsureCanAccessAsync(UserId userId, CancellationToken cancellationToken = default) =>
        currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken);

    public async Task<Result<WeeklyCheckInUserProfile>> GetAsync(UserId userId, CancellationToken cancellationToken = default) {
        Result<UserWeeklyCheckInProfileModel> profileResult = await userProfileReadService
            .GetWeeklyCheckInProfileAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        if (profileResult.IsFailure) {
            return Result.Failure<WeeklyCheckInUserProfile>(profileResult.Error);
        }

        return Result.Success(new WeeklyCheckInUserProfile(profileResult.Value.DailyCalorieTarget));
    }
}
