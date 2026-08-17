using FoodDiary.Application.Abstractions.Audit.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Dashboard.Models;
using FoodDiary.Application.Dashboard.Services;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Dashboard.Queries.GetDietologistClientDashboard;

public sealed class GetDietologistClientDashboardQueryHandler(
    IDietologistDashboardAccessService accessService,
    IDashboardSnapshotBuilder snapshotBuilder,
    IAuditEntryWriter auditWriter,
    IUnitOfWork unitOfWork,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetDietologistClientDashboardQuery, Result<DashboardSnapshotModel>> {
    public async Task<Result<DashboardSnapshotModel>> Handle(
        GetDietologistClientDashboardQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> dietologistResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId, currentUserAccessService, cancellationToken).ConfigureAwait(false);
        if (dietologistResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<DashboardSnapshotModel>(dietologistResult);
        }

        Result<UserId> clientResult = UserIdParser.Parse(
            query.ClientUserId,
            Errors.Validation.Invalid(nameof(query.ClientUserId), "Client user id must not be empty."));
        if (clientResult.IsFailure) {
            return UserIdParser.ToFailure<DashboardSnapshotModel>(clientResult);
        }

        Result<DietologistPermissionsReadModel> permissionsResult = await accessService.GetPermissionsAsync(
            dietologistResult.Value, clientResult.Value, cancellationToken).ConfigureAwait(false);
        if (permissionsResult.IsFailure) {
            return Result.Failure<DashboardSnapshotModel>(permissionsResult.Error);
        }

        DietologistPermissionsReadModel permissions = permissionsResult.Value;
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
                    permissions.ShareStatistics,
                    permissions.ShareMeals,
                    permissions.ShareWeight,
                    permissions.ShareWaist,
                    permissions.ShareHydration,
                    permissions.ShareFasting,
                    IncludeAdvice: false,
                    IncludeLayout: false,
                    IncludeExercise: false,
                    IncludeTdee: false,
                    IncludeCycle: false)),
            cancellationToken).ConfigureAwait(false);
        if (dashboardResult.IsFailure) {
            return dashboardResult;
        }

        await RecordRequiredAccessAuditAsync(
            dietologistResult.Value,
            query.ClientUserId,
            cancellationToken).ConfigureAwait(false);
        return Result.Success(ApplyPermissions(dashboardResult.Value, permissions));
    }

    private async Task RecordRequiredAccessAuditAsync(
        UserId dietologistUserId,
        Guid clientUserId,
        CancellationToken cancellationToken) {
        await auditWriter.AddAsync(
            dietologistUserId,
            clientUserId,
            "dietologist.dashboard.accessed",
            "ClientDashboard",
            clientUserId.ToString(),
            metadata: null,
            cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static DashboardSnapshotModel ApplyPermissions(
        DashboardSnapshotModel dashboard,
        DietologistPermissionsReadModel permissions) =>
        dashboard with {
            DailyGoal = permissions.ShareStatistics ? dashboard.DailyGoal : 0,
            WeeklyCalorieGoal = permissions.ShareStatistics ? dashboard.WeeklyCalorieGoal : 0,
            Statistics = permissions.ShareStatistics
                ? dashboard.Statistics
                : new DashboardStatisticsModel(
                    TotalCalories: 0,
                    AverageProteins: 0,
                    AverageFats: 0,
                    AverageCarbs: 0,
                    AverageFiber: 0,
                    ProteinGoal: null,
                    FatGoal: null,
                    CarbGoal: null,
                    FiberGoal: null),
            WeeklyCalories = permissions.ShareStatistics ? dashboard.WeeklyCalories : [],
            Weight = permissions.ShareWeight
                ? dashboard.Weight
                : new DashboardWeightModel(Latest: null, Previous: null, DesiredWeightKg: null),
            Waist = permissions.ShareWaist
                ? dashboard.Waist
                : new DashboardWaistModel(Latest: null, Previous: null, DesiredWaistCm: null),
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
