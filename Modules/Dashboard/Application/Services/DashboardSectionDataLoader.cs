using FoodDiary.Modules.Users.Contracts.Common.Validation;
using FoodDiary.Modules.Exercises.Contracts.Queries.ReadExerciseCalories;
using FoodDiary.Modules.Fasting.Contracts.Queries.ReadCurrentFasting;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;
using FoodDiary.Modules.Dashboard.Application.Abstractions.Common;
using FoodDiary.Modules.Dashboard.Application.Abstractions.Models;
using FoodDiary.Modules.Dashboard.Application.Internal;
using FoodDiary.Modules.Cycles.Contracts.Models;
using FoodDiary.Modules.Cycles.Contracts.Queries.GetCurrentCycle;
using FoodDiary.Modules.DailyAdvices.Contracts.Models;
using FoodDiary.Modules.DailyAdvices.Contracts.Queries.GetDailyAdvice;
using FoodDiary.Modules.Dashboard.Application.Models;
using FoodDiary.Modules.Fasting.Contracts.Read.Models;
using FoodDiary.Modules.Tdee.Contracts.Models;
using FoodDiary.Modules.Tdee.Contracts.Queries.GetTdeeInsight;
using FoodDiary.Modules.Dashboard.Application.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Dashboard.Application.Services;

internal sealed class DashboardSectionDataLoader(
    ISender sender,
    IDashboardUserContextService dashboardUserContextService,

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

        if (!LocalCalendar.TryResolve(request.TimeZoneId, request.TimeZoneOffsetMinutes, out TimeZoneInfo timeZone)) {
            return Result.Failure<DashboardBuildContext>(Errors.Validation.Invalid(nameof(request.TimeZoneId), "Unknown time zone."));
        }
        DateTime normalizedDate = UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(request.Date);
        DateTime normalizedDateTo = UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(request.DateTo ?? request.Date);
        if (!TemporalRangePolicy.IsPeriodWithinLimit(normalizedDate, normalizedDateTo)) {
            return Result.Failure<DashboardBuildContext>(Errors.Validation.Invalid(nameof(request.DateTo), $"Calendar dates must be ordered and span at most {MaxPeriodDays} days."));
        }
        int periodDays = TemporalRangePolicy.GetInclusiveDayCount(normalizedDate, normalizedDateTo);
        int trendDays = Math.Clamp(request.TrendDays <= 0 ? DefaultTrendDays : request.TrendDays, 1, MaxTrendDays);
        DateTime dayStart;
        DateTime dayEndStart;
        DateTime dayEndExclusive;
        DateTime trendStart;
        DashboardCalendarRange calendar;
        try {
            var date = DateOnly.FromDateTime(normalizedDate);
            var dateTo = DateOnly.FromDateTime(normalizedDateTo);
            DateOnly trendDate = date.AddDays(-(trendDays - 1));
            dayStart = LocalCalendar.StartOfDayUtc(date, timeZone);
            dayEndStart = LocalCalendar.StartOfDayUtc(dateTo, timeZone);
            dayEndExclusive = LocalCalendar.StartOfDayUtc(dateTo.AddDays(1), timeZone);
            trendStart = LocalCalendar.StartOfDayUtc(trendDate, timeZone);
            if (LocalCalendar.DateAt(dayStart, timeZone) != date || dayEndExclusive <= dayEndStart) {
                return Result.Failure<DashboardBuildContext>(Errors.Validation.Invalid(nameof(request.Date), "Calendar day does not exist in this time zone."));
            }
            calendar = new DashboardCalendarRange(normalizedDate, normalizedDateTo, trendDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), timeZone);
        } catch (ArgumentOutOfRangeException) {
            return Result.Failure<DashboardBuildContext>(Errors.Validation.Invalid(nameof(request.Date), "Date range is outside supported boundaries."));
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
            currentUser,
            calendar));
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
            cancellationToken,
            context.Calendar);

    public async Task<Result<DailyAdviceModel>?> LoadAdviceAsync(
        DashboardBuildContext context,
        CancellationToken cancellationToken) {
        return context.Sections.IncludeAdvice
            ? await sender.Send(new GetDailyAdviceQuery(context.UserId, context.Calendar.Date, context.Locale), cancellationToken).ConfigureAwait(false)
            : null;
    }

    public Task<FastingSessionModel?> LoadFastingAsync(
        DashboardBuildContext context,
        CancellationToken cancellationToken) =>
        context.Sections.IncludeFasting
            ? sender.Send(new ReadCurrentFastingQuery(context.UserId), cancellationToken)
            : Task.FromResult<FastingSessionModel?>(null);

    public Task<double> LoadCaloriesBurnedAsync(
        DashboardBuildContext context,
        CancellationToken cancellationToken) =>
        context.Sections.IncludeExercise
            ? sender.Send(new ReadExerciseCaloriesQuery(context.UserId, context.Calendar.Date), cancellationToken)
            : Task.FromResult(0d);

    public async Task<Result<TdeeInsightModel>?> LoadTdeeAsync(
        DashboardSnapshotRequest request,
        DashboardBuildContext context,
        CancellationToken cancellationToken) {
        return context.Sections.IncludeTdee
            ? await sender.Send(new GetTdeeInsightQuery(request.UserId, DateOnly.FromDateTime(context.Calendar.Date), request.TimeZoneId, request.TimeZoneOffsetMinutes), cancellationToken).ConfigureAwait(false)
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
            .Send(new GetCurrentCycleQuery(request.UserId, DateOnly.FromDateTime(context.Calendar.Date)), cancellationToken)
            .ConfigureAwait(false);
        return result.IsSuccess && result.Value?.HideFromDashboard == true
            ? Result.Success<CycleModel?>(value: null)
            : result;
    }

}
