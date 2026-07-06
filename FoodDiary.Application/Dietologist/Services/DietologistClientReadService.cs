using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Dashboard.Models;
using FoodDiary.Application.Dashboard.Services;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Application.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Services;

public sealed class DietologistClientReadService(
    IDietologistInvitationReadModelRepository invitationRepository,
    IDashboardSnapshotBuilder snapshotBuilder,
    ICurrentUserAccessService currentUserAccessService,
    IDietologistUserContextService dietologistUserContextService)
    : IDietologistClientReadService {
    public async Task<Result<DashboardSnapshotModel>> GetDashboardAsync(
        UserId dietologistUserId,
        Guid clientUserId,
        DateTime date,
        DateTime? dateTo,
        string locale,
        int trendDays,
        int page,
        int pageSize,
        CancellationToken cancellationToken) {
        Error? currentUserAccessError = await currentUserAccessService.EnsureCanAccessAsync(dietologistUserId, cancellationToken).ConfigureAwait(false);
        if (currentUserAccessError is not null) {
            return Result.Failure<DashboardSnapshotModel>(currentUserAccessError);
        }

        var client = new UserId(clientUserId);
        Result<DietologistPermissionsModel> accessResult = await DietologistAccessPolicy.EnsureCanAccessClientReadModelAsync(
            invitationRepository, dietologistUserId, client, cancellationToken).ConfigureAwait(false);

        if (accessResult.IsFailure) {
            return Result.Failure<DashboardSnapshotModel>(accessResult.Error);
        }

        DietologistPermissionsModel permissions = accessResult.Value;
        if (!DietologistAccessPolicy.HasAnyDashboardPermission(permissions)) {
            return Result.Failure<DashboardSnapshotModel>(Errors.Dietologist.PermissionDenied);
        }

        Result<DashboardSnapshotModel> dashboardResult = await snapshotBuilder.BuildAsync(
            new DashboardSnapshotRequest(
                clientUserId,
                date,
                dateTo,
                locale,
                trendDays,
                page,
                pageSize,
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

    public async Task<Result<UserModel>> GetGoalsAsync(
        UserId dietologistUserId,
        Guid clientUserId,
        CancellationToken cancellationToken) {
        Result<string> dietologistResult = await dietologistUserContextService
            .GetAccessibleUserEmailAsync(dietologistUserId, cancellationToken)
            .ConfigureAwait(false);
        if (dietologistResult.IsFailure) {
            return Result.Failure<UserModel>(dietologistResult.Error);
        }

        var client = new UserId(clientUserId);
        Result<DietologistPermissionsModel> accessResult = await DietologistAccessPolicy.EnsureCanAccessClientReadModelAsync(
            invitationRepository, dietologistUserId, client, cancellationToken).ConfigureAwait(false);

        if (accessResult.IsFailure) {
            return Result.Failure<UserModel>(accessResult.Error);
        }

        Error? permissionError = DietologistAccessPolicy.EnsurePermission(accessResult.Value, "Goals");
        if (permissionError is not null) {
            return Result.Failure<UserModel>(permissionError);
        }

        return await dietologistUserContextService.GetUserModelByIdAsync(client, cancellationToken).ConfigureAwait(false);
    }

    private static DashboardSnapshotModel ApplyPermissions(
        DashboardSnapshotModel dashboard,
        DietologistPermissionsModel permissions) {
        return dashboard with {
            DailyGoal = permissions.ShareStatistics ? dashboard.DailyGoal : 0,
            WeeklyCalorieGoal = permissions.ShareStatistics ? dashboard.WeeklyCalorieGoal : 0,
            Statistics = permissions.ShareStatistics
                ? dashboard.Statistics
                : new DashboardStatisticsModel(0, 0, 0, 0, 0, ProteinGoal: null, FatGoal: null, CarbGoal: null, FiberGoal: null),
            WeeklyCalories = permissions.ShareStatistics ? dashboard.WeeklyCalories : [],
            Weight = permissions.ShareWeight ? dashboard.Weight : new DashboardWeightModel(Latest: null, Previous: null, Desired: null),
            Waist = permissions.ShareWaist ? dashboard.Waist : new DashboardWaistModel(Latest: null, Previous: null, Desired: null),
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
