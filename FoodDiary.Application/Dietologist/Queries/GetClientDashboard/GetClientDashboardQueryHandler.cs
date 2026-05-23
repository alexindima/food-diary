using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Dashboard.Models;
using FoodDiary.Application.Dashboard.Queries.GetDashboardSnapshot;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Dietologist.Queries.GetClientDashboard;

public class GetClientDashboardQueryHandler(
    IDietologistInvitationRepository invitationRepository,
    ISender mediator)
    : IQueryHandler<GetClientDashboardQuery, Result<DashboardSnapshotModel>> {
    public async Task<Result<DashboardSnapshotModel>> Handle(
        GetClientDashboardQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<DashboardSnapshotModel>(Errors.Authentication.InvalidToken);
        }

        var dietologistUserId = new UserId(query.UserId!.Value);
        var clientUserId = new UserId(query.ClientUserId);

        var accessResult = await DietologistAccessPolicy.EnsureCanAccessClientAsync(
            invitationRepository, dietologistUserId, clientUserId, cancellationToken);

        if (accessResult.IsFailure) {
            return Result.Failure<DashboardSnapshotModel>(accessResult.Error);
        }

        var permissions = accessResult.Value;
        if (!DietologistAccessPolicy.HasAnyDashboardPermission(permissions)) {
            return Result.Failure<DashboardSnapshotModel>(Errors.Dietologist.PermissionDenied);
        }

        var dashboardQuery = new GetDashboardSnapshotQuery(
            query.ClientUserId,
            query.Date,
            query.Page,
            query.PageSize,
            query.Locale,
            query.TrendDays);

        var dashboardResult = await mediator.Send(dashboardQuery, cancellationToken);
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
            CurrentCycle = null
        };
    }
}
