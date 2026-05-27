using System.Text.Json;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Time;
using FoodDiary.Application.Abstractions.Exercises.Common;
using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Application.Abstractions.Hydration.Common;
using FoodDiary.Application.Fasting.Mappings;
using FoodDiary.Application.Abstractions.WaistEntries.Common;
using FoodDiary.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Application.Consumptions.Queries.GetConsumptions;
using FoodDiary.Application.Cycles.Queries.GetCurrentCycle;
using FoodDiary.Application.DailyAdvices.Queries.GetDailyAdvice;
using FoodDiary.Application.Dashboard.Models;
using FoodDiary.Application.Hydration.Models;
using FoodDiary.Application.Users.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Statistics.Queries.GetStatistics;
using FoodDiary.Application.Tdee.Queries.GetTdeeInsight;
using FoodDiary.Application.WaistEntries.Models;
using FoodDiary.Application.WaistEntries.Queries.GetWaistSummaries;
using FoodDiary.Application.WeightEntries.Models;
using FoodDiary.Application.WeightEntries.Queries.GetWeightSummaries;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Mediator;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Application.Dashboard.Services;

public interface IDashboardSnapshotBuilder {
    Task<Result<DashboardSnapshotModel>> BuildAsync(
        DashboardSnapshotRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record DashboardSnapshotRequest(
    Guid UserId,
    DateTime Date,
    DateTime? DateTo,
    string Locale,
    int TrendDays,
    int Page,
    int PageSize,
    DashboardSnapshotSections? Sections = null);

public sealed record DashboardSnapshotSections(
    bool IncludeStatistics,
    bool IncludeMeals,
    bool IncludeWeight,
    bool IncludeWaist,
    bool IncludeHydration,
    bool IncludeFasting,
    bool IncludeAdvice,
    bool IncludeLayout,
    bool IncludeExercise,
    bool IncludeTdee,
    bool IncludeCycle) {
    public static DashboardSnapshotSections All { get; } = new(
        IncludeStatistics: true,
        IncludeMeals: true,
        IncludeWeight: true,
        IncludeWaist: true,
        IncludeHydration: true,
        IncludeFasting: true,
        IncludeAdvice: true,
        IncludeLayout: true,
        IncludeExercise: true,
        IncludeTdee: true,
        IncludeCycle: true);
}

public class DashboardSnapshotBuilder(
    ISender sender,
    IUserRepository userRepository,
    IWeightEntryRepository weightEntryRepository,
    IWaistEntryRepository waistEntryRepository,
    IHydrationEntryRepository hydrationEntryRepository,
    IFastingOccurrenceRepository fastingOccurrenceRepository,
    IExerciseEntryRepository exerciseEntryRepository,
    ILogger<DashboardSnapshotBuilder> logger) : IDashboardSnapshotBuilder {

    public async Task<Result<DashboardSnapshotModel>> BuildAsync(
        DashboardSnapshotRequest request,
        CancellationToken cancellationToken = default) {
        if (request.UserId == Guid.Empty) {
            return Result.Failure<DashboardSnapshotModel>(
                Errors.Validation.Invalid(nameof(request.UserId), "User id must not be empty."));
        }

        var dayStart = UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(request.Date);
        var requestedEnd = request.DateTo ?? request.Date;
        var dayEndStart = UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(requestedEnd);
        if (dayEndStart < dayStart) {
            return Result.Failure<DashboardSnapshotModel>(
                Errors.Validation.Invalid(nameof(request.DateTo), "DateTo must be later than or equal to Date."));
        }

        var dayEnd = dayEndStart.AddDays(1).AddTicks(-1);
        var periodDays = Math.Max(1, (dayEndStart.Date - dayStart.Date).Days + 1);
        var sections = request.Sections ?? DashboardSnapshotSections.All;
        var userId = new UserId(request.UserId);
        var locale = string.IsNullOrWhiteSpace(request.Locale) ? "en" : request.Locale;
        var trendDays = Math.Clamp(request.TrendDays <= 0 ? 7 : request.TrendDays, 1, 31);
        var trendStart = dayStart.AddDays(-(trendDays - 1));

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        var accessError = CurrentUserAccessPolicy.EnsureCanAccess(user);
        if (accessError is not null) {
            return Result.Failure<DashboardSnapshotModel>(accessError);
        }

        var currentUser = user!;

        var statistics = new DashboardStatisticsModel(0, 0, 0, 0, 0, null, null, null, null);
        IReadOnlyList<DailyCaloriesModel> weeklyCalories = [];
        if (sections.IncludeStatistics) {
            var statsResult = await sender.Send(new GetStatisticsQuery(
                request.UserId, dayStart, dayEnd, periodDays), cancellationToken);
            if (statsResult.IsFailure) return Result.Failure<DashboardSnapshotModel>(statsResult.Error);

            var weeklyFrom = periodDays == 1 ? dayStart.AddDays(-6) : dayStart;
            var weeklyStatsResult = await sender.Send(new GetStatisticsQuery(
                request.UserId, weeklyFrom, dayEnd, 1), cancellationToken);
            if (weeklyStatsResult.IsFailure) return Result.Failure<DashboardSnapshotModel>(weeklyStatsResult.Error);

            statistics = DashboardMapping.ToStatisticsModel(statsResult.Value.FirstOrDefault(), currentUser);
            weeklyCalories = DashboardMapping.ToWeeklyCalories(weeklyStatsResult.Value);
        }

        var meals = new DashboardMealsModel([], 0);
        if (sections.IncludeMeals) {
            var mealsResult = await sender.Send(new GetConsumptionsQuery(
                request.UserId, request.Page, request.PageSize, dayStart, dayEnd), cancellationToken);
            if (mealsResult.IsFailure) return Result.Failure<DashboardSnapshotModel>(mealsResult.Error);

            meals = new DashboardMealsModel(mealsResult.Value.Data, mealsResult.Value.TotalItems);
        }

        var weight = new DashboardWeightModel(null, null, null);
        IReadOnlyList<WeightEntrySummaryModel> weightTrend = [];
        if (sections.IncludeWeight) {
            var weightEntries = await weightEntryRepository.GetEntriesAsync(
                userId, dateFrom: null, dateTo: dayEndStart, limit: 2, descending: true, cancellationToken: cancellationToken);
            weight = DashboardMapping.ToWeightModel(weightEntries, currentUser.DesiredWeight);

            var weightTrendResult = await sender.Send(
                new GetWeightSummariesQuery(userId, trendStart, dayStart, 1), cancellationToken);
            weightTrend = weightTrendResult.IsSuccess ? weightTrendResult.Value : [];
        }

        var waist = new DashboardWaistModel(null, null, null);
        IReadOnlyList<WaistEntrySummaryModel> waistTrend = [];
        if (sections.IncludeWaist) {
            var waistEntries = await waistEntryRepository.GetEntriesAsync(
                userId, dateFrom: null, dateTo: dayEndStart, limit: 2, descending: true, cancellationToken: cancellationToken);
            waist = DashboardMapping.ToWaistModel(waistEntries, currentUser.DesiredWaist);

            var waistTrendResult = await sender.Send(
                new GetWaistSummariesQuery(userId, trendStart, dayStart, 1), cancellationToken);
            waistTrend = waistTrendResult.IsSuccess ? waistTrendResult.Value : [];
        }

        HydrationDailyModel? hydration = null;
        if (sections.IncludeHydration) {
            var hydrationTotals = await hydrationEntryRepository.GetDailyTotalsAsync(userId, dayStart, dayEndStart, cancellationToken);
            var totalMl = hydrationTotals.Sum(x => x.TotalMl);
            var dailyGoal = currentUser.HydrationGoal ?? currentUser.WaterGoal;
            hydration = new HydrationDailyModel(
                dayStart,
                totalMl,
                dailyGoal is null ? null : dailyGoal * periodDays);
        }

        var adviceResult = sections.IncludeAdvice
            ? await sender.Send(new GetDailyAdviceQuery(userId, dayStart, locale), cancellationToken)
            : null;

        var currentFastingSession = sections.IncludeFasting
            ? await fastingOccurrenceRepository.GetCurrentAsync(userId, cancellationToken: cancellationToken)
            : null;

        var layout = sections.IncludeLayout ? ParseDashboardLayout(currentUser.DashboardLayoutJson, userId) : null;

        var caloriesBurned = sections.IncludeExercise
            ? await exerciseEntryRepository.GetTotalCaloriesBurnedAsync(userId, dayStart, cancellationToken)
            : 0;

        var tdeeInsightResult = sections.IncludeTdee
            ? await sender.Send(new GetTdeeInsightQuery(request.UserId), cancellationToken)
            : null;
        var currentCycleResult = sections.IncludeCycle
            ? await sender.Send(new GetCurrentCycleQuery(request.UserId), cancellationToken)
            : null;

        return Result.Success(new DashboardSnapshotModel(
            dayStart,
            dayEndStart,
            currentUser.GetCalorieTargetForDate(request.Date) ?? 0,
            currentUser.GetWeeklyCalorieTarget(),
            statistics,
            weeklyCalories,
            weight,
            waist,
            meals,
            hydration,
            adviceResult?.IsSuccess == true ? adviceResult.Value : null,
            currentFastingSession?.ToModel(),
            weightTrend,
            waistTrend,
            layout,
            Math.Round(caloriesBurned, 1),
            tdeeInsightResult?.IsSuccess == true ? tdeeInsightResult.Value : null,
            currentCycleResult?.IsSuccess == true ? currentCycleResult.Value : null));
    }

    private DashboardLayoutModel? ParseDashboardLayout(string? json, UserId userId) {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try {
            return JsonSerializer.Deserialize<DashboardLayoutModel>(json);
        } catch (JsonException ex) {
            logger.LogWarning(ex, "Failed to deserialize dashboard layout JSON for user {UserId}", userId);
            return null;
        }
    }
}
