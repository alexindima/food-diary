using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Dashboard.Common;
using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Application.Exercises.Common;
using FoodDiary.Application.Dashboard.Internal;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Application.Cycles.Queries.GetCurrentCycle;
using FoodDiary.Application.DailyAdvices.Models;
using FoodDiary.Application.DailyAdvices.Queries.GetDailyAdvice;
using FoodDiary.Application.Dashboard.Models;
using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Application.Abstractions.Fasting.Models;
using FoodDiary.Application.Tdee.Models;
using FoodDiary.Application.Tdee.Queries.GetTdeeInsight;
using FoodDiary.Application.Dashboard.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Dashboard.Services;

internal sealed class DashboardSectionDataLoader(
    ISender sender,
    IDashboardUserContextService dashboardUserContextService,
    IFastingReadService fastingReadService,
    IExerciseEntryReadService exerciseEntryReadService,
    IDashboardReadService dashboardReadService) : IDashboardSectionDataLoader {
    internal const int MaxPeriodDays = TemporalRangePolicy.MaxPeriodDays;
    private const int DefaultPageSize = 10;
    private const int DefaultTrendDays = 7;
    private const int MaxTrendDays = 31;
    private const int MinTimeZoneOffsetMinutes = -840;
    private const int MaxTimeZoneOffsetMinutes = 840;

    public async Task<Result<DashboardBuildContext>> CreateBuildContextAsync(
        DashboardSnapshotRequest request,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(
            request.UserId,
            Errors.Validation.Invalid(nameof(request.UserId), "User id must not be empty."));
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<DashboardBuildContext>(userIdResult);
        }

        if (request.TimeZoneOffsetMinutes is < MinTimeZoneOffsetMinutes or > MaxTimeZoneOffsetMinutes) {
            return Result.Failure<DashboardBuildContext>(
                Errors.Validation.Invalid(
                    nameof(request.TimeZoneOffsetMinutes),
                    "Time-zone offset must be between -840 and 840 minutes."));
        }

        TimeSpan timeZoneOffset = request.TimeZoneOffsetMinutes.HasValue
            ? TimeSpan.FromMinutes(request.TimeZoneOffsetMinutes.Value)
            : TimeSpan.Zero;
        DateTime normalizedDate = UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(request.Date);
        DateTime normalizedDateTo = UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(request.DateTo ?? request.Date);
        if (!TemporalRangePolicy.TrySubtract(normalizedDate, timeZoneOffset, out DateTime dayStart) ||
            !TemporalRangePolicy.TrySubtract(normalizedDateTo, timeZoneOffset, out DateTime dayEndStart)) {
            return Result.Failure<DashboardBuildContext>(
                Errors.Validation.Invalid(nameof(request.Date), "Date and time-zone offset produce an unsupported range."));
        }

        if (dayEndStart < dayStart) {
            return Result.Failure<DashboardBuildContext>(
                Errors.Validation.Invalid(nameof(request.DateTo), "DateTo must be later than or equal to Date."));
        }

        if (!TemporalRangePolicy.IsPeriodWithinLimit(dayStart, dayEndStart)) {
            return Result.Failure<DashboardBuildContext>(
                Errors.Validation.Invalid(
                    nameof(request.DateTo),
                    $"Dashboard period must not exceed {MaxPeriodDays} days."));
        }

        int periodDays = TemporalRangePolicy.GetInclusiveDayCount(dayStart, dayEndStart);
        int trendDays = Math.Clamp(request.TrendDays <= 0 ? DefaultTrendDays : request.TrendDays, 1, MaxTrendDays);
        if (!TemporalRangePolicy.TryAddDays(dayEndStart, 1, out DateTime dayEndExclusive) ||
            !TemporalRangePolicy.TryAddDays(dayStart, -(trendDays - 1), out DateTime trendStart)) {
            return Result.Failure<DashboardBuildContext>(
                Errors.Validation.Invalid(nameof(request.Date), "Date range is too close to the supported DateTime boundary."));
        }

        UserId userId = userIdResult.Value;
        DashboardUserContextModel currentUser;
        if (request.UserContext is not null) {
            if (request.UserContext.Id != userId.Value) {
                return Result.Failure<DashboardBuildContext>(
                    Errors.Validation.Invalid(nameof(request.UserContext), "Dashboard user context must match the requested user."));
            }

            currentUser = request.UserContext;
        } else {
            Result<DashboardUserContextModel> userResult = await dashboardUserContextService
                .GetAccessibleDashboardUserAsync(userId, cancellationToken)
                .ConfigureAwait(false);
            if (userResult.IsFailure) {
                return Result.Failure<DashboardBuildContext>(userResult.Error);
            }

            currentUser = userResult.Value;
        }

        return Result.Success(new DashboardBuildContext(
            userId,
            dayStart,
            dayEndStart,
            dayEndExclusive.AddTicks(-1),
            periodDays,
            string.IsNullOrWhiteSpace(request.Locale) ? "en" : request.Locale,
            PaginationPolicy.NormalizePage(request.Page),
            PaginationPolicy.NormalizePageSize(request.PageSize, DefaultPageSize),
            trendDays,
            trendStart,
            request.Sections ?? DashboardSnapshotSections.All,
            currentUser));
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

    public Task<FastingSessionModel?> LoadFastingAsync(
        DashboardBuildContext context,
        CancellationToken cancellationToken) =>
        context.Sections.IncludeFasting
            ? fastingReadService.GetCurrentAsync(context.UserId, cancellationToken)
            : Task.FromResult<FastingSessionModel?>(null);

    public Task<double> LoadCaloriesBurnedAsync(
        DashboardBuildContext context,
        CancellationToken cancellationToken) =>
        context.Sections.IncludeExercise
            ? exerciseEntryReadService.GetTotalCaloriesBurnedAsync(context.UserId, context.DayStart, cancellationToken)
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
        if (!context.Sections.IncludeCycle) {
            return null;
        }

        Result<CycleModel?> result = await sender
            .Send(new GetCurrentCycleQuery(request.UserId), cancellationToken)
            .ConfigureAwait(false);
        return result.IsSuccess && result.Value?.HideFromDashboard == true
            ? Result.Success<CycleModel?>(value: null)
            : result;
    }

}
