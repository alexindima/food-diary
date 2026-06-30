using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Dashboard.Common;
using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Application.Abstractions.Exercises.Common;
using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Application.Common.Time;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Application.Cycles.Queries.GetCurrentCycle;
using FoodDiary.Application.DailyAdvices.Models;
using FoodDiary.Application.DailyAdvices.Queries.GetDailyAdvice;
using FoodDiary.Application.Dashboard.Models;
using FoodDiary.Application.Hydration.Models;
using FoodDiary.Application.Tdee.Models;
using FoodDiary.Application.Tdee.Queries.GetTdeeInsight;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.WaistEntries.Models;
using FoodDiary.Application.WeightEntries.Models;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Dashboard.Services;

internal sealed class DashboardSectionDataLoader(
    ISender sender,
    IUserRepository userRepository,
    IFastingOccurrenceRepository fastingOccurrenceRepository,
    IExerciseEntryRepository exerciseEntryRepository,
    IDashboardStatisticsReadService statisticsReadService,
    IDashboardBodyReadService bodyReadService,
    IDashboardMealsReadService mealsReadService) : IDashboardSectionDataLoader {
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 100;
    private const int DefaultTrendDays = 7;
    private const int MaxTrendDays = 31;

    public async Task<Result<DashboardBuildContext>> CreateBuildContextAsync(
        DashboardSnapshotRequest request,
        CancellationToken cancellationToken) {
        if (request.UserId == Guid.Empty) {
            return Result.Failure<DashboardBuildContext>(
                Errors.Validation.Invalid(nameof(request.UserId), "User id must not be empty."));
        }

        DateTime dayStart = UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(request.Date);
        DateTime dayEndStart = UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(request.DateTo ?? request.Date);
        if (dayEndStart < dayStart) {
            return Result.Failure<DashboardBuildContext>(
                Errors.Validation.Invalid(nameof(request.DateTo), "DateTo must be later than or equal to Date."));
        }

        var userId = new UserId(request.UserId);
        User? user = await userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        Error? accessError = CurrentUserAccessPolicy.EnsureCanAccess(user);
        if (accessError is not null) {
            return Result.Failure<DashboardBuildContext>(accessError);
        }

        int trendDays = Math.Clamp(request.TrendDays <= 0 ? DefaultTrendDays : request.TrendDays, 1, MaxTrendDays);
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

    public async Task<Result<DashboardStatisticsSection>> LoadStatisticsAsync(
        DashboardSnapshotRequest request,
        DashboardBuildContext context,
        CancellationToken cancellationToken) {
        if (!context.Sections.IncludeStatistics) {
            return Result.Success(new DashboardStatisticsSection(
                new DashboardStatisticsModel(0, 0, 0, 0, 0, ProteinGoal: null, FatGoal: null, CarbGoal: null, FiberGoal: null),
                []));
        }

        Result<IReadOnlyList<DashboardStatisticsBucketReadModel>> statsResult = await statisticsReadService.GetStatisticsAsync(
            context.UserId, context.DayStart, context.DayEnd, context.PeriodDays, cancellationToken).ConfigureAwait(false);
        if (statsResult.IsFailure) {
            return Result.Failure<DashboardStatisticsSection>(statsResult.Error);
        }

        DateTime weeklyFrom = context.PeriodDays == 1 ? context.DayStart.AddDays(-6) : context.DayStart;
        Result<IReadOnlyList<DashboardStatisticsBucketReadModel>> weeklyStatsResult = await statisticsReadService.GetStatisticsAsync(
            context.UserId, weeklyFrom, context.DayEnd, 1, cancellationToken).ConfigureAwait(false);
        if (weeklyStatsResult.IsFailure) {
            return Result.Failure<DashboardStatisticsSection>(weeklyStatsResult.Error);
        }

        DashboardStatisticsBucketReadModel? statistics = statsResult.Value.Count > 0 ? statsResult.Value[0] : null;
        return Result.Success(new DashboardStatisticsSection(
            DashboardMapping.ToStatisticsModel(statistics, context.CurrentUser),
            DashboardMapping.ToWeeklyCalories(weeklyStatsResult.Value)));
    }

    public async Task<Result<DashboardMealsModel>> LoadMealsAsync(
        DashboardSnapshotRequest request,
        DashboardBuildContext context,
        CancellationToken cancellationToken) {
        if (!context.Sections.IncludeMeals) {
            return Result.Success(new DashboardMealsModel([], 0));
        }

        Result<DashboardMealsReadModel> mealsResult = await mealsReadService.GetMealsAsync(
            context.UserId,
            context.Page,
            context.PageSize,
            context.DayStart,
            context.DayEnd,
            cancellationToken).ConfigureAwait(false);
        return mealsResult.IsFailure
            ? Result.Failure<DashboardMealsModel>(mealsResult.Error)
            : Result.Success(DashboardMapping.ToMealsModel(mealsResult.Value));
    }

    public async Task<DashboardBodySection> LoadBodyAsync(
        DashboardBuildContext context,
        CancellationToken cancellationToken) {
        DashboardBodyReadModel readModel = await bodyReadService.GetBodyAsync(
            context.UserId,
            context.DayStart,
            context.DayEndStart,
            context.TrendStart,
            1,
            context.Sections.IncludeWeight,
            context.Sections.IncludeWaist,
            context.Sections.IncludeHydration,
            cancellationToken).ConfigureAwait(false);

        DashboardWeightModel weight = context.Sections.IncludeWeight
            ? DashboardMapping.ToWeightModel(readModel.LatestWeightEntries, context.CurrentUser.DesiredWeight)
            : new DashboardWeightModel(Latest: null, Previous: null, Desired: null);
        DashboardWaistModel waist = context.Sections.IncludeWaist
            ? DashboardMapping.ToWaistModel(readModel.LatestWaistEntries, context.CurrentUser.DesiredWaist)
            : new DashboardWaistModel(Latest: null, Previous: null, Desired: null);
        IReadOnlyList<WeightEntrySummaryModel> weightTrend = context.Sections.IncludeWeight
            ? DashboardMapping.ToWeightTrend(readModel.WeightTrend)
            : [];
        IReadOnlyList<WaistEntrySummaryModel> waistTrend = context.Sections.IncludeWaist
            ? DashboardMapping.ToWaistTrend(readModel.WaistTrend)
            : [];
        double? dailyGoal = context.CurrentUser.HydrationGoal ?? context.CurrentUser.WaterGoal;
        HydrationDailyModel? hydration = context.Sections.IncludeHydration
            ? new HydrationDailyModel(
                context.DayStart,
                readModel.HydrationTotalMl,
                dailyGoal is null ? null : dailyGoal * context.PeriodDays)
            : null;

        return new DashboardBodySection(weight, waist, weightTrend, waistTrend, hydration);
    }

    public async Task<Result<DailyAdviceModel>?> LoadAdviceAsync(
        DashboardBuildContext context,
        CancellationToken cancellationToken) {
        return context.Sections.IncludeAdvice
            ? await sender.Send(new GetDailyAdviceQuery(context.UserId, context.DayStart, context.Locale), cancellationToken).ConfigureAwait(false)
            : null;
    }

    public Task<FastingOccurrence?> LoadFastingAsync(
        DashboardBuildContext context,
        CancellationToken cancellationToken) =>
        context.Sections.IncludeFasting
            ? fastingOccurrenceRepository.GetCurrentAsync(context.UserId, cancellationToken: cancellationToken)
            : Task.FromResult<FastingOccurrence?>(null);

    public Task<double> LoadCaloriesBurnedAsync(
        DashboardBuildContext context,
        CancellationToken cancellationToken) =>
        context.Sections.IncludeExercise
            ? exerciseEntryRepository.GetTotalCaloriesBurnedAsync(context.UserId, context.DayStart, cancellationToken)
            : Task.FromResult(0d);

    public async Task<Result<TdeeInsightModel>?> LoadTdeeAsync(
        DashboardSnapshotRequest request,
        DashboardBuildContext context,
        CancellationToken cancellationToken) {
        return context.Sections.IncludeTdee
            ? await sender.Send(new GetTdeeInsightQuery(request.UserId), cancellationToken).ConfigureAwait(false)
            : null;
    }

    public async Task<Result<CycleModel?>?> LoadCycleAsync(
        DashboardSnapshotRequest request,
        DashboardBuildContext context,
        CancellationToken cancellationToken) {
        return context.Sections.IncludeCycle
            ? await sender.Send(new GetCurrentCycleQuery(request.UserId), cancellationToken).ConfigureAwait(false)
            : null;
    }

}
