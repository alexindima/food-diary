using System.Text.Json;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Application.Abstractions.Exercises.Common;
using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Application.Abstractions.Hydration.Common;
using FoodDiary.Application.Abstractions.WaistEntries.Common;
using FoodDiary.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Application.DailyAdvices.Models;
using FoodDiary.Application.Dashboard.Common;
using FoodDiary.Application.Dashboard.Models;
using FoodDiary.Application.Fasting.Mappings;
using FoodDiary.Application.Hydration.Models;
using FoodDiary.Application.Tdee.Models;
using FoodDiary.Application.Users.Models;
using FoodDiary.Application.WaistEntries.Models;
using FoodDiary.Application.WeightEntries.Models;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Mediator;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Application.Dashboard.Services;

public class DashboardSnapshotBuilder : IDashboardSnapshotBuilder {
    private readonly IDashboardSectionDataLoader _dataLoader;
    private readonly ILogger<DashboardSnapshotBuilder> _logger;

    internal DashboardSnapshotBuilder(
        IDashboardSectionDataLoader dataLoader,
        ILogger<DashboardSnapshotBuilder> logger) {
        _dataLoader = dataLoader;
        _logger = logger;
    }

    public DashboardSnapshotBuilder(
        ISender sender,
        IDashboardUserContextService dashboardUserContextService,
        IWeightEntryRepository weightEntryRepository,
        IWaistEntryRepository waistEntryRepository,
        IHydrationEntryRepository hydrationEntryRepository,
        IFastingOccurrenceRepository fastingOccurrenceRepository,
        IExerciseEntryRepository exerciseEntryRepository,
        ILogger<DashboardSnapshotBuilder> logger)
        : this(
            new DashboardSectionDataLoader(
                sender,
                dashboardUserContextService,
                fastingOccurrenceRepository,
                exerciseEntryRepository,
                new ComposedDashboardReadService(
                    new MediatorDashboardStatisticsReadService(sender),
                    new RepositoryDashboardBodyReadService(weightEntryRepository, waistEntryRepository, hydrationEntryRepository),
                    new MediatorDashboardMealsReadService(sender))),
            logger) {
    }

    public async Task<Result<DashboardSnapshotModel>> BuildAsync(
        DashboardSnapshotRequest request,
        CancellationToken cancellationToken = default) {
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
        FastingOccurrence? fastingSession = await _dataLoader.LoadFastingAsync(context, cancellationToken).ConfigureAwait(false);
        DashboardLayoutModel? layout = context.Sections.IncludeLayout
            ? ParseDashboardLayout(context.CurrentUser.DashboardLayoutJson, context.UserId)
            : null;
        double caloriesBurned = await _dataLoader.LoadCaloriesBurnedAsync(context, cancellationToken).ConfigureAwait(false);
        Result<TdeeInsightModel>? tdeeInsightResult = await _dataLoader.LoadTdeeAsync(request, context, cancellationToken).ConfigureAwait(false);
        Result<CycleModel?>? currentCycleResult = await _dataLoader.LoadCycleAsync(request, context, cancellationToken).ConfigureAwait(false);

        return Result.Success(new DashboardSnapshotModel(
            context.DayStart,
            context.DayEndStart,
            context.CurrentUser.GetCalorieTargetForDate(request.Date) ?? 0,
            context.CurrentUser.GetWeeklyCalorieTarget(),
            BuildStatistics(readModel.Statistics, context),
            DashboardMapping.ToWeeklyCalories(readModel.WeeklyStatistics),
            BuildWeight(readModel.Body, context),
            BuildWaist(readModel.Body, context),
            DashboardMapping.ToMealsModel(readModel.Meals),
            BuildHydration(readModel.Body, context),
            adviceResult?.IsSuccess == true ? adviceResult.Value : null,
            fastingSession?.ToModel(),
            BuildWeightTrend(readModel.Body, context),
            BuildWaistTrend(readModel.Body, context),
            layout,
            Math.Round(caloriesBurned, 1, MidpointRounding.ToEven),
            tdeeInsightResult?.IsSuccess == true ? tdeeInsightResult.Value : null,
            currentCycleResult?.IsSuccess == true ? currentCycleResult.Value : null));
    }

    private static DashboardStatisticsModel BuildStatistics(DashboardReadModel readModel, DashboardBuildContext context) =>
        BuildStatistics(readModel.Statistics, context);

    private static DashboardStatisticsModel BuildStatistics(
        IReadOnlyList<DashboardStatisticsBucketReadModel> statistics,
        DashboardBuildContext context) {
        if (!context.Sections.IncludeStatistics) {
            return new DashboardStatisticsModel(0, 0, 0, 0, 0, ProteinGoal: null, FatGoal: null, CarbGoal: null, FiberGoal: null);
        }

        DashboardStatisticsBucketReadModel? first = statistics.Count > 0 ? statistics[0] : null;
        return DashboardMapping.ToStatisticsModel(first, context.CurrentUser);
    }

    private static DashboardWeightModel BuildWeight(DashboardBodyReadModel body, DashboardBuildContext context) =>
        context.Sections.IncludeWeight
            ? DashboardMapping.ToWeightModel(body.LatestWeightEntries, context.CurrentUser.DesiredWeight)
            : new DashboardWeightModel(Latest: null, Previous: null, Desired: null);

    private static DashboardWaistModel BuildWaist(DashboardBodyReadModel body, DashboardBuildContext context) =>
        context.Sections.IncludeWaist
            ? DashboardMapping.ToWaistModel(body.LatestWaistEntries, context.CurrentUser.DesiredWaist)
            : new DashboardWaistModel(Latest: null, Previous: null, Desired: null);

    private static IReadOnlyList<WeightEntrySummaryModel> BuildWeightTrend(DashboardBodyReadModel body, DashboardBuildContext context) =>
        context.Sections.IncludeWeight ? DashboardMapping.ToWeightTrend(body.WeightTrend) : [];

    private static IReadOnlyList<WaistEntrySummaryModel> BuildWaistTrend(DashboardBodyReadModel body, DashboardBuildContext context) =>
        context.Sections.IncludeWaist ? DashboardMapping.ToWaistTrend(body.WaistTrend) : [];

    private static HydrationDailyModel? BuildHydration(DashboardBodyReadModel body, DashboardBuildContext context) {
        if (!context.Sections.IncludeHydration) {
            return null;
        }

        double? dailyGoal = context.CurrentUser.HydrationGoal ?? context.CurrentUser.WaterGoal;
        return new HydrationDailyModel(
            context.DayStart,
            body.HydrationTotalMl,
            dailyGoal is null ? null : dailyGoal * context.PeriodDays);
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
