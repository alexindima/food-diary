using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Dashboard.Common;
using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Application.Abstractions.Exercises.Common;
using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Application.Common.Time;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Application.Cycles.Queries.GetCurrentCycle;
using FoodDiary.Application.DailyAdvices.Models;
using FoodDiary.Application.DailyAdvices.Queries.GetDailyAdvice;
using FoodDiary.Application.Tdee.Models;
using FoodDiary.Application.Tdee.Queries.GetTdeeInsight;
using FoodDiary.Application.Dashboard.Common;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Dashboard.Services;

internal sealed class DashboardSectionDataLoader(
    ISender sender,
    IDashboardUserContextService dashboardUserContextService,
    IFastingOccurrenceReadRepository fastingOccurrenceRepository,
    IExerciseEntryRepository exerciseEntryRepository,
    IDashboardReadService dashboardReadService) : IDashboardSectionDataLoader {
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
        Result<User> userResult = await dashboardUserContextService.GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure<DashboardBuildContext>(userResult.Error);
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
            userResult.Value));
    }

    public Task<Result<DashboardReadModel>> LoadDashboardDataAsync(
        DashboardBuildContext context,
        CancellationToken cancellationToken) =>
        dashboardReadService.GetSnapshotDataAsync(
            context.UserId,
            context.DayStart,
            context.DayEnd,
            context.TrendStart,
            context.PeriodDays,
            context.Page,
            context.PageSize,
            new DashboardReadSections(
                context.Sections.IncludeStatistics,
                context.Sections.IncludeMeals,
                context.Sections.IncludeWeight,
                context.Sections.IncludeWaist,
                context.Sections.IncludeHydration),
            cancellationToken);

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
