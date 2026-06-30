using System.Text.Json;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Exercises.Common;
using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Application.Abstractions.Hydration.Common;
using FoodDiary.Application.Abstractions.WaistEntries.Common;
using FoodDiary.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Application.DailyAdvices.Models;
using FoodDiary.Application.Dashboard.Models;
using FoodDiary.Application.Fasting.Mappings;
using FoodDiary.Application.Tdee.Models;
using FoodDiary.Application.Users.Models;
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
        IUserRepository userRepository,
        IWeightEntryRepository weightEntryRepository,
        IWaistEntryRepository waistEntryRepository,
        IHydrationEntryRepository hydrationEntryRepository,
        IFastingOccurrenceRepository fastingOccurrenceRepository,
        IExerciseEntryRepository exerciseEntryRepository,
        ILogger<DashboardSnapshotBuilder> logger)
        : this(
            new DashboardSectionDataLoader(
                sender,
                userRepository,
                fastingOccurrenceRepository,
                exerciseEntryRepository,
                new MediatorDashboardStatisticsReadService(sender),
                new RepositoryDashboardBodyReadService(weightEntryRepository, waistEntryRepository, hydrationEntryRepository),
                new MediatorDashboardMealsReadService(sender)),
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
        Result<DashboardStatisticsSection> statisticsResult = await _dataLoader.LoadStatisticsAsync(request, context, cancellationToken).ConfigureAwait(false);
        if (statisticsResult.IsFailure) {
            return Result.Failure<DashboardSnapshotModel>(statisticsResult.Error);
        }

        Result<DashboardMealsModel> mealsResult = await _dataLoader.LoadMealsAsync(request, context, cancellationToken).ConfigureAwait(false);
        if (mealsResult.IsFailure) {
            return Result.Failure<DashboardSnapshotModel>(mealsResult.Error);
        }

        DashboardBodySection body = await _dataLoader.LoadBodyAsync(context, cancellationToken).ConfigureAwait(false);
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
            statisticsResult.Value.Statistics,
            statisticsResult.Value.WeeklyCalories,
            body.Weight,
            body.Waist,
            mealsResult.Value,
            body.Hydration,
            adviceResult?.IsSuccess == true ? adviceResult.Value : null,
            fastingSession?.ToModel(),
            body.WeightTrend,
            body.WaistTrend,
            layout,
            Math.Round(caloriesBurned, 1, MidpointRounding.ToEven),
            tdeeInsightResult?.IsSuccess == true ? tdeeInsightResult.Value : null,
            currentCycleResult?.IsSuccess == true ? currentCycleResult.Value : null));
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
