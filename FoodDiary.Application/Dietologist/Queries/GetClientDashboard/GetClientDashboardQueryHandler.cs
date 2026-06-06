using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Dashboard.Models;
using FoodDiary.Application.Dashboard.Services;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Queries.GetClientDashboard;

public class GetClientDashboardQueryHandler(
    IDietologistInvitationRepository invitationRepository,
    IDashboardSnapshotBuilder snapshotBuilder,
    IUserRepository userRepository)
    : IQueryHandler<GetClientDashboardQuery, Result<DashboardSnapshotModel>> {
    public async Task<Result<DashboardSnapshotModel>> Handle(
        GetClientDashboardQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<DashboardSnapshotModel>(Errors.Authentication.InvalidToken);
        }

        var dietologistUserId = new UserId(query.UserId!.Value);
        Error? currentUserAccessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(
            userRepository, dietologistUserId, cancellationToken).ConfigureAwait(false);
        if (currentUserAccessError is not null) {
            return Result.Failure<DashboardSnapshotModel>(currentUserAccessError);
        }

        var clientUserId = new UserId(query.ClientUserId);

        Result<DietologistPermissionsModel> accessResult = await DietologistAccessPolicy.EnsureCanAccessClientAsync(
            invitationRepository, dietologistUserId, clientUserId, cancellationToken).ConfigureAwait(false);

        if (accessResult.IsFailure) {
            return Result.Failure<DashboardSnapshotModel>(accessResult.Error);
        }

        DietologistPermissionsModel permissions = accessResult.Value;
        if (!DietologistAccessPolicy.HasAnyDashboardPermission(permissions)) {
            return Result.Failure<DashboardSnapshotModel>(Errors.Dietologist.PermissionDenied);
        }

        Result<DashboardSnapshotModel> dashboardResult = await snapshotBuilder.BuildAsync(
            new DashboardSnapshotRequest(
                query.ClientUserId,
                query.Date,
                query.DateTo,
                query.Locale,
                query.TrendDays,
                query.Page,
                query.PageSize,
                new DashboardSnapshotSections(
                    IncludeStatistics: permissions.ShareStatistics,
                    IncludeMeals: permissions.ShareMeals,
                    IncludeWeight: permissions.ShareWeight,
                    IncludeWaist: permissions.ShareWaist,
                    IncludeHydration: permissions.ShareHydration,
                    IncludeFasting: permissions.ShareFasting,
                    IncludeAdvice: false,
                    IncludeLayout: false,
                    IncludeExercise: false,
                    IncludeTdee: false,
                    IncludeCycle: false)),
            cancellationToken).ConfigureAwait(false);
        if (dashboardResult.IsFailure) {
            return dashboardResult;
        }

        return Result.Success(ApplyPermissions(dashboardResult.Value, permissions));
    }

    private static DashboardSnapshotModel ApplyPermissions(
        DashboardSnapshotModel dashboard,
        DietologistPermissionsModel permissions) {
        return dashboard with {
            DailyGoal = permissions.ShareStatistics ? dashboard.DailyGoal : 0,
            WeeklyCalorieGoal = permissions.ShareStatistics ? dashboard.WeeklyCalorieGoal : 0,
            Statistics = permissions.ShareStatistics
                ? dashboard.Statistics
                : new DashboardStatisticsModel(0, 0, 0, 0, 0, null, null, null, null),
            WeeklyCalories = permissions.ShareStatistics ? dashboard.WeeklyCalories : [],
            Weight = permissions.ShareWeight ? dashboard.Weight : new DashboardWeightModel(null, null, null),
            Waist = permissions.ShareWaist ? dashboard.Waist : new DashboardWaistModel(null, null, null),
            Meals = permissions.ShareMeals ? dashboard.Meals : new DashboardMealsModel([], 0),
            Hydration = permissions.ShareHydration ? dashboard.Hydration : null,
            Advice = null,
            CurrentFastingSession = permissions.ShareFasting ? dashboard.CurrentFastingSession : null,
            WeightTrend = permissions.ShareWeight ? dashboard.WeightTrend : [],
            WaistTrend = permissions.ShareWaist ? dashboard.WaistTrend : [],
            DashboardLayout = null,
            CaloriesBurned = 0,
            TdeeInsight = null,
            CurrentCycle = null,
        };
    }
}
