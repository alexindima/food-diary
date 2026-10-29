using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Dashboard.Application.Common;
using FoodDiary.Modules.Dashboard.Application.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Dashboard.Application.Services;

internal sealed class DashboardUserContextService(
    ICurrentUserAccessService currentUserAccessService,
    IUserDashboardProfileReadService userProfileReadService) : IDashboardUserContextService {
    public Task<Error?> EnsureCanAccessAsync(UserId userId, CancellationToken cancellationToken = default) =>
        currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken);

    public async Task<Result<DashboardUserContextModel>> GetAccessibleDashboardUserAsync(
        UserId userId,
        CancellationToken cancellationToken,
        DateOnly? goalsFrom = null,
        DateOnly? goalsTo = null) {
        Result<UserDashboardProfileModel> profileResult = await userProfileReadService
            .GetDashboardProfileAsync(userId, cancellationToken, goalsFrom, goalsTo)
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
            profile.CalorieSchedule,
            profile.TimeZoneId,
            profile.GoalHistory);
}
