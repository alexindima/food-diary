using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Dashboard.Common;
using FoodDiary.Application.Dashboard.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dashboard.Services;

internal sealed class DashboardUserContextService(
    ICurrentUserAccessService currentUserAccessService,
    IUserDashboardProfileReadService userProfileReadService) : IDashboardUserContextService {
    public Task<Error?> EnsureCanAccessAsync(UserId userId, CancellationToken cancellationToken = default) =>
        currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken);

    public async Task<Result<DashboardUserContextModel>> GetAccessibleDashboardUserAsync(
        UserId userId,
        CancellationToken cancellationToken) {
        Result<UserDashboardProfileModel> profileResult = await userProfileReadService
            .GetDashboardProfileAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        return profileResult.IsFailure
            ? Result.Failure<DashboardUserContextModel>(profileResult.Error)
            : Result.Success(ToDashboardUserContextModel(profileResult.Value));
    }

    private static DashboardUserContextModel ToDashboardUserContextModel(UserDashboardProfileModel profile) =>
        new(
            profile.Id,
            profile.Email,
            profile.Language,
            profile.DashboardLayoutJson,
            profile.DesiredWeightKg,
            profile.DesiredWaistCm,
            profile.HydrationGoal,
            profile.WaterGoal,
            profile.ProteinTarget,
            profile.FatTarget,
            profile.CarbTarget,
            profile.FiberTarget,
            profile.CalorieSchedule);
}
