using System.Text.Json;
using FoodDiary.Results;
using FoodDiary.Modules.Dashboard.Application.Abstractions.Models;
using FoodDiary.Modules.Dashboard.Contracts.Models;
using FoodDiary.Modules.Dashboard.Application.Abstractions.Common;
using FoodDiary.Modules.Cycles.Contracts.Models;
using FoodDiary.Modules.DailyAdvices.Contracts.Models;
using FoodDiary.Modules.Dashboard.Application.Common;
using FoodDiary.Modules.Fasting.Contracts.Read.Models;
using FoodDiary.Modules.Hydration.Contracts.Models;
using FoodDiary.Modules.Tdee.Contracts.Models;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Models;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Mediator;
using Microsoft.Extensions.Logging;
using FoodDiary.Application.Contracts.Common.Abstractions.Persistence;

namespace FoodDiary.Modules.Dashboard.Application.Services;

public sealed class DashboardSnapshotBuilder : IDashboardSnapshotBuilder {
    private readonly IDashboardSectionDataLoader _dataLoader;
    private readonly ILogger<DashboardSnapshotBuilder> _logger;
    private readonly IReadSnapshotExecutor? _snapshotExecutor;
    internal static readonly ReadSnapshotBudget SnapshotBudget = new(TimeSpan.FromSeconds(15), MaximumQueries: 32);

    internal DashboardSnapshotBuilder(
        IDashboardSectionDataLoader dataLoader,
        ILogger<DashboardSnapshotBuilder> logger,
        IReadSnapshotExecutor? snapshotExecutor = null) {
        _dataLoader = dataLoader;
        _logger = logger;
        _snapshotExecutor = snapshotExecutor;
    }

    public DashboardSnapshotBuilder(
        ISender sender,
        IDashboardUserContextService dashboardUserContextService,

        IDashboardReadService dashboardReadService,
        ILogger<DashboardSnapshotBuilder> logger)
        : this(new DashboardSectionDataLoader(
            sender, dashboardUserContextService, dashboardReadService), logger) {
    }

    public Task<Result<DashboardSnapshotModel>> BuildAsync(
        DashboardSnapshotRequest request,
        CancellationToken cancellationToken = default) => _snapshotExecutor is null
        ? BuildSnapshotAsync(request, cancellationToken)
        : _snapshotExecutor.ExecuteAsync(SnapshotBudget, token => BuildSnapshotAsync(request, token), cancellationToken);

    private async Task<Result<DashboardSnapshotModel>> BuildSnapshotAsync(DashboardSnapshotRequest request, CancellationToken cancellationToken) {
        Result<DashboardBuildContext> contextResult = await _dataLoader.CreateBuildContextAsync(request, cancellationToken).ConfigureAwait(false);
        if (contextResult.IsFailure) {
            return Result.Failure<DashboardSnapshotModel>(contextResult.Error);
        }

        DashboardBuildContext context = contextResult.Value;
        Result<DashboardReadModel> readResult = await _dataLoader.LoadDashboardDataAsync(context, cancellationToken).ConfigureAwait(false);
        if (readResult.IsFailure) {
            return Result.Failure<DashboardSnapshotModel>(readResult.Error);
        }

        DashboardReadModel readModel = readResult.Value;
        Result<DailyAdviceModel>? adviceResult = await _dataLoader.LoadAdviceAsync(context, cancellationToken).ConfigureAwait(false);
        FastingSessionModel? fastingSession = await _dataLoader.LoadFastingAsync(context, cancellationToken).ConfigureAwait(false);
        DashboardLayoutModel? layout = context.Sections.IncludeLayout
            ? ParseDashboardLayout(context.CurrentUser.DashboardLayoutJson, context.UserId)
            : null;
        double caloriesBurned = await _dataLoader.LoadCaloriesBurnedAsync(context, cancellationToken).ConfigureAwait(false);
        Result<TdeeInsightModel>? tdeeInsightResult = await _dataLoader.LoadTdeeAsync(request, context, cancellationToken).ConfigureAwait(false);
        Result<CycleModel?>? currentCycleResult = await _dataLoader.LoadCycleAsync(request, context, cancellationToken).ConfigureAwait(false);
        UserDailyGoals[] dailyGoals = GetDailyGoals(context);

        return Result.Success(new DashboardSnapshotModel(
            context.DayStart,
            context.DayEndStart,
            context.CurrentUser.GetCalorieTargetForDate(context.Calendar.Date) ?? 0,
            GetPeriodCalorieGoal(context),
            BuildStatistics(readModel.Statistics, context, dailyGoals),
            DashboardMapping.ToWeeklyCalories(readModel.WeeklyStatistics),
            BuildWeight(readModel.Body, context),
            BuildWaist(readModel.Body, context),
            DashboardMapping.ToMealsModel(readModel.Meals),
            BuildHydration(readModel.Body, context, dailyGoals),
            adviceResult?.IsSuccess == true ? adviceResult.Value : null,
            fastingSession,
            BuildWeightTrend(readModel.Body, context),
            BuildWaistTrend(readModel.Body, context),
            layout,
            Math.Round(caloriesBurned, 1, MidpointRounding.ToEven),
            tdeeInsightResult?.IsSuccess == true ? tdeeInsightResult.Value : null,
            currentCycleResult?.IsSuccess == true && currentCycleResult.Value?.HideFromDashboard != true ? currentCycleResult.Value : null,
            CycleHidden: currentCycleResult?.IsSuccess == true && currentCycleResult.Value?.HideFromDashboard == true,
            HydrationHistory: context.Sections.IncludeHydration ? readModel.Body.HydrationHistory : null));
    }

    private static DashboardStatisticsModel BuildStatistics(
        IReadOnlyList<DashboardStatisticsBucketReadModel> statistics,
        DashboardBuildContext context,
        IReadOnlyList<UserDailyGoals> dailyGoals) {
        if (!context.Sections.IncludeStatistics) {
            return new DashboardStatisticsModel(0, 0, 0, 0, 0, ProteinGoal: null, FatGoal: null, CarbGoal: null, FiberGoal: null);
        }

        DashboardStatisticsBucketReadModel? first = statistics.Count > 0 ? statistics[0] : null;
        DashboardStatisticsModel statisticsModel = DashboardMapping.ToStatisticsModel(first, context.CurrentUser);
        return statisticsModel with {
            ProteinGoal = AverageGoal(dailyGoals.Select(goals => goals.ProteinTarget)),
            FatGoal = AverageGoal(dailyGoals.Select(goals => goals.FatTarget)),
            CarbGoal = AverageGoal(dailyGoals.Select(goals => goals.CarbTarget)),
            FiberGoal = AverageGoal(dailyGoals.Select(goals => goals.FiberTarget)),
        };
    }

    private static DashboardWeightModel BuildWeight(DashboardBodyReadModel body, DashboardBuildContext context) =>
        context.Sections.IncludeWeight
            ? DashboardMapping.ToWeightModel(body.LatestWeightEntries, context.CurrentUser.DesiredWeightKg)
            : new DashboardWeightModel(Latest: null, Previous: null, DesiredWeightKg: null);

    private static DashboardWaistModel BuildWaist(DashboardBodyReadModel body, DashboardBuildContext context) =>
        context.Sections.IncludeWaist
            ? DashboardMapping.ToWaistModel(body.LatestWaistEntries, context.CurrentUser.DesiredWaistCm)
            : new DashboardWaistModel(Latest: null, Previous: null, DesiredWaistCm: null);

    private static IReadOnlyList<WeightEntrySummaryModel> BuildWeightTrend(DashboardBodyReadModel body, DashboardBuildContext context) =>
        context.Sections.IncludeWeight ? DashboardMapping.ToWeightTrend(body.WeightTrend) : [];

    private static IReadOnlyList<WaistEntrySummaryModel> BuildWaistTrend(DashboardBodyReadModel body, DashboardBuildContext context) =>
        context.Sections.IncludeWaist ? DashboardMapping.ToWaistTrend(body.WaistTrend) : [];

    private static HydrationDailyModel? BuildHydration(DashboardBodyReadModel body, DashboardBuildContext context,
        IReadOnlyList<UserDailyGoals> dailyGoals) {
        if (!context.Sections.IncludeHydration) {
            return null;
        }

        double?[] waterGoals = [.. dailyGoals.Select(goals => goals.EffectiveWaterGoal)];
        return new HydrationDailyModel(
            context.DayStart,
            body.HydrationTotalMl,
            waterGoals.Any(goal => goal is null) ? null : waterGoals.Sum());
    }

    private static UserDailyGoals[] GetDailyGoals(DashboardBuildContext context) {
        var start = DateOnly.FromDateTime(context.Calendar.Date);
        return [.. Enumerable.Range(0, context.PeriodDays).Select(offset => context.CurrentUser.GetGoalsForDate(start.AddDays(offset)))];
    }

    private static double? AverageGoal(IEnumerable<double?> goals) {
        double?[] values = [.. goals];
        return values.Any(value => value is null) ? null : values.Average();
    }

    private static double GetPeriodCalorieGoal(DashboardBuildContext context) {
        var end = DateOnly.FromDateTime(context.Calendar.DateTo);
        DateOnly start = context.PeriodDays == 1 ? end.AddDays(-6) : DateOnly.FromDateTime(context.Calendar.Date);
        return Enumerable.Range(0, end.DayNumber - start.DayNumber + 1).Sum(offset => {
            DateOnly day = start.AddDays(offset);
            return context.CurrentUser.GetGoalsForDate(day).CalorieSchedule.GetTargetForDate(day.ToDateTime(TimeOnly.MinValue)) ?? 0;
        });
    }

    private DashboardLayoutModel? ParseDashboardLayout(string? json, UserId userId) {
        if (string.IsNullOrWhiteSpace(json)) {
            return null;
        }

        try {
            return JsonSerializer.Deserialize<DashboardLayoutModel>(json);
        } catch (JsonException ex) {
            _logger.LogWarning(ex, "Failed to deserialize dashboard layout JSON for user {UserId}", userId);
            return null;
        }
    }
}
