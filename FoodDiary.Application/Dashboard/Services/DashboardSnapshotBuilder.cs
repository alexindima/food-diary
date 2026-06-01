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
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Application.Cycles.Queries.GetCurrentCycle;
using FoodDiary.Application.DailyAdvices.Queries.GetDailyAdvice;
using FoodDiary.Application.DailyAdvices.Models;
using FoodDiary.Application.Dashboard.Models;
using FoodDiary.Application.Hydration.Models;
using FoodDiary.Application.Tdee.Models;
using FoodDiary.Application.Users.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Statistics.Queries.GetStatistics;
using FoodDiary.Application.Tdee.Queries.GetTdeeInsight;
using FoodDiary.Application.WaistEntries.Models;
using FoodDiary.Application.WaistEntries.Queries.GetWaistSummaries;
using FoodDiary.Application.WeightEntries.Models;
using FoodDiary.Application.WeightEntries.Queries.GetWeightSummaries;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Mediator;
using Microsoft.Extensions.Logging;
using User = FoodDiary.Domain.Entities.Users.User;

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
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 100;
    private const int DefaultTrendDays = 7;
    private const int MaxTrendDays = 31;

    private sealed record DashboardBuildContext(
        UserId UserId,
        DateTime DayStart,
        DateTime DayEndStart,
        DateTime DayEnd,
        int PeriodDays,
        string Locale,
        int Page,
        int PageSize,
        int TrendDays,
        DateTime TrendStart,
        DashboardSnapshotSections Sections,
        User CurrentUser);

    private sealed record DashboardStatisticsSection(
        DashboardStatisticsModel Statistics,
        IReadOnlyList<DailyCaloriesModel> WeeklyCalories);

    private sealed record DashboardBodySection(
        DashboardWeightModel Weight,
        DashboardWaistModel Waist,
        IReadOnlyList<WeightEntrySummaryModel> WeightTrend,
        IReadOnlyList<WaistEntrySummaryModel> WaistTrend);

    public async Task<Result<DashboardSnapshotModel>> BuildAsync(
        DashboardSnapshotRequest request,
        CancellationToken cancellationToken = default) {
        var contextResult = await CreateBuildContextAsync(request, cancellationToken).ConfigureAwait(false);
        if (contextResult.IsFailure) {
            return Result.Failure<DashboardSnapshotModel>(contextResult.Error);
        }

        var context = contextResult.Value;
        var statisticsResult = await BuildStatisticsSectionAsync(request, context, cancellationToken).ConfigureAwait(false);
        if (statisticsResult.IsFailure) return Result.Failure<DashboardSnapshotModel>(statisticsResult.Error);

        var mealsResult = await BuildMealsSectionAsync(request, context, cancellationToken).ConfigureAwait(false);
        if (mealsResult.IsFailure) return Result.Failure<DashboardSnapshotModel>(mealsResult.Error);

        var body = await BuildBodySectionAsync(context, cancellationToken).ConfigureAwait(false);
        var hydration = await BuildHydrationSectionAsync(context, cancellationToken).ConfigureAwait(false);
        var adviceResult = await BuildAdviceSectionAsync(context, cancellationToken).ConfigureAwait(false);
        var fastingSession = await BuildFastingSectionAsync(context, cancellationToken).ConfigureAwait(false);
        var layout = context.Sections.IncludeLayout
            ? ParseDashboardLayout(context.CurrentUser.DashboardLayoutJson, context.UserId)
            : null;
        var caloriesBurned = await BuildExerciseSectionAsync(context, cancellationToken).ConfigureAwait(false);
        var tdeeInsightResult = await BuildTdeeSectionAsync(request, context, cancellationToken).ConfigureAwait(false);
        var currentCycleResult = await BuildCycleSectionAsync(request, context, cancellationToken).ConfigureAwait(false);

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
            hydration,
            adviceResult?.IsSuccess == true ? adviceResult.Value : null,
            fastingSession?.ToModel(),
            body.WeightTrend,
            body.WaistTrend,
            layout,
            Math.Round(caloriesBurned, 1),
            tdeeInsightResult?.IsSuccess == true ? tdeeInsightResult.Value : null,
            currentCycleResult?.IsSuccess == true ? currentCycleResult.Value : null));
    }

    private async Task<Result<DashboardBuildContext>> CreateBuildContextAsync(
        DashboardSnapshotRequest request,
        CancellationToken cancellationToken) {
        if (request.UserId == Guid.Empty) {
            return Result.Failure<DashboardBuildContext>(
                Errors.Validation.Invalid(nameof(request.UserId), "User id must not be empty."));
        }

        var dayStart = UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(request.Date);
        var dayEndStart = UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(request.DateTo ?? request.Date);
        if (dayEndStart < dayStart) {
            return Result.Failure<DashboardBuildContext>(
                Errors.Validation.Invalid(nameof(request.DateTo), "DateTo must be later than or equal to Date."));
        }

        var userId = new UserId(request.UserId);
        var user = await userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        var accessError = CurrentUserAccessPolicy.EnsureCanAccess(user);
        if (accessError is not null) {
            return Result.Failure<DashboardBuildContext>(accessError);
        }

        var trendDays = Math.Clamp(request.TrendDays <= 0 ? DefaultTrendDays : request.TrendDays, 1, MaxTrendDays);
        return Result.Success(new DashboardBuildContext(
            userId,
            dayStart,
            dayEndStart,
            dayEndStart.AddDays(1).AddTicks(-1),
            Math.Max(1, (dayEndStart.Date - dayStart.Date).Days + 1),
            string.IsNullOrWhiteSpace(request.Locale) ? "en" : request.Locale,
            request.Page <= 0 ? DefaultPage : request.Page,
            request.PageSize <= 0 ? DefaultPageSize : Math.Min(request.PageSize, MaxPageSize),
            trendDays,
            dayStart.AddDays(-(trendDays - 1)),
            request.Sections ?? DashboardSnapshotSections.All,
            user!));
    }

    private async Task<Result<DashboardStatisticsSection>> BuildStatisticsSectionAsync(
        DashboardSnapshotRequest request,
        DashboardBuildContext context,
        CancellationToken cancellationToken) {
        if (!context.Sections.IncludeStatistics) {
            return Result.Success(new DashboardStatisticsSection(
                new DashboardStatisticsModel(0, 0, 0, 0, 0, null, null, null, null),
                []));
        }

        var statsResult = await sender.Send(new GetStatisticsQuery(
            request.UserId, context.DayStart, context.DayEnd, context.PeriodDays), cancellationToken).ConfigureAwait(false);
        if (statsResult.IsFailure) return Result.Failure<DashboardStatisticsSection>(statsResult.Error);

        var weeklyFrom = context.PeriodDays == 1 ? context.DayStart.AddDays(-6) : context.DayStart;
        var weeklyStatsResult = await sender.Send(new GetStatisticsQuery(
            request.UserId, weeklyFrom, context.DayEnd, 1), cancellationToken).ConfigureAwait(false);
        if (weeklyStatsResult.IsFailure) return Result.Failure<DashboardStatisticsSection>(weeklyStatsResult.Error);

        return Result.Success(new DashboardStatisticsSection(
            DashboardMapping.ToStatisticsModel(statsResult.Value.FirstOrDefault(), context.CurrentUser),
            DashboardMapping.ToWeeklyCalories(weeklyStatsResult.Value)));
    }

    private async Task<Result<DashboardMealsModel>> BuildMealsSectionAsync(
        DashboardSnapshotRequest request,
        DashboardBuildContext context,
        CancellationToken cancellationToken) {
        if (!context.Sections.IncludeMeals) {
            return Result.Success(new DashboardMealsModel([], 0));
        }

        var mealsResult = await sender.Send(new GetConsumptionsQuery(
            request.UserId, context.Page, context.PageSize, context.DayStart, context.DayEnd), cancellationToken).ConfigureAwait(false);
        return mealsResult.IsFailure
            ? Result.Failure<DashboardMealsModel>(mealsResult.Error)
            : Result.Success(new DashboardMealsModel(mealsResult.Value.Data, mealsResult.Value.TotalItems));
    }

    private async Task<DashboardBodySection> BuildBodySectionAsync(
        DashboardBuildContext context,
        CancellationToken cancellationToken) {
        var weight = await BuildWeightSectionAsync(context, cancellationToken).ConfigureAwait(false);
        var waist = await BuildWaistSectionAsync(context, cancellationToken).ConfigureAwait(false);
        return new DashboardBodySection(weight.Model, waist.Model, weight.Trend, waist.Trend);
    }

    private async Task<(DashboardWeightModel Model, IReadOnlyList<WeightEntrySummaryModel> Trend)> BuildWeightSectionAsync(
        DashboardBuildContext context,
        CancellationToken cancellationToken) {
        if (!context.Sections.IncludeWeight) {
            return (new DashboardWeightModel(null, null, null), []);
        }

        var weightEntries = await weightEntryRepository.GetEntriesAsync(
            context.UserId, null, context.DayEndStart, 2, true, cancellationToken).ConfigureAwait(false);
        var weightTrendResult = await sender.Send(
            new GetWeightSummariesQuery(context.UserId, context.TrendStart, context.DayStart, 1), cancellationToken).ConfigureAwait(false);
        return (
            DashboardMapping.ToWeightModel(weightEntries, context.CurrentUser.DesiredWeight),
            weightTrendResult.IsSuccess ? weightTrendResult.Value : []);
    }

    private async Task<(DashboardWaistModel Model, IReadOnlyList<WaistEntrySummaryModel> Trend)> BuildWaistSectionAsync(
        DashboardBuildContext context,
        CancellationToken cancellationToken) {
        if (!context.Sections.IncludeWaist) {
            return (new DashboardWaistModel(null, null, null), []);
        }

        var waistEntries = await waistEntryRepository.GetEntriesAsync(
            context.UserId, null, context.DayEndStart, 2, true, cancellationToken).ConfigureAwait(false);
        var waistTrendResult = await sender.Send(
            new GetWaistSummariesQuery(context.UserId, context.TrendStart, context.DayStart, 1), cancellationToken).ConfigureAwait(false);
        return (
            DashboardMapping.ToWaistModel(waistEntries, context.CurrentUser.DesiredWaist),
            waistTrendResult.IsSuccess ? waistTrendResult.Value : []);
    }

    private async Task<HydrationDailyModel?> BuildHydrationSectionAsync(
        DashboardBuildContext context,
        CancellationToken cancellationToken) {
        if (!context.Sections.IncludeHydration) {
            return null;
        }

        var hydrationTotals = await hydrationEntryRepository.GetDailyTotalsAsync(
            context.UserId, context.DayStart, context.DayEndStart, cancellationToken).ConfigureAwait(false);
        var dailyGoal = context.CurrentUser.HydrationGoal ?? context.CurrentUser.WaterGoal;
        return new HydrationDailyModel(
            context.DayStart,
            hydrationTotals.Sum(x => x.TotalMl),
            dailyGoal is null ? null : dailyGoal * context.PeriodDays);
    }

    private async Task<Result<DailyAdviceModel>?> BuildAdviceSectionAsync(
        DashboardBuildContext context,
        CancellationToken cancellationToken) {
        return context.Sections.IncludeAdvice
            ? await sender.Send(new GetDailyAdviceQuery(context.UserId, context.DayStart, context.Locale), cancellationToken).ConfigureAwait(false)
            : null;
    }

    private Task<FastingOccurrence?> BuildFastingSectionAsync(
        DashboardBuildContext context,
        CancellationToken cancellationToken) =>
        context.Sections.IncludeFasting
            ? fastingOccurrenceRepository.GetCurrentAsync(context.UserId, cancellationToken: cancellationToken)
            : Task.FromResult<FastingOccurrence?>(null);

    private Task<double> BuildExerciseSectionAsync(
        DashboardBuildContext context,
        CancellationToken cancellationToken) =>
        context.Sections.IncludeExercise
            ? exerciseEntryRepository.GetTotalCaloriesBurnedAsync(context.UserId, context.DayStart, cancellationToken)
            : Task.FromResult(0d);

    private async Task<Result<TdeeInsightModel>?> BuildTdeeSectionAsync(
        DashboardSnapshotRequest request,
        DashboardBuildContext context,
        CancellationToken cancellationToken) {
        return context.Sections.IncludeTdee
            ? await sender.Send(new GetTdeeInsightQuery(request.UserId), cancellationToken).ConfigureAwait(false)
            : null;
    }

    private async Task<Result<CycleModel?>?> BuildCycleSectionAsync(
        DashboardSnapshotRequest request,
        DashboardBuildContext context,
        CancellationToken cancellationToken) {
        return context.Sections.IncludeCycle
            ? await sender.Send(new GetCurrentCycleQuery(request.UserId), cancellationToken).ConfigureAwait(false)
            : null;
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
