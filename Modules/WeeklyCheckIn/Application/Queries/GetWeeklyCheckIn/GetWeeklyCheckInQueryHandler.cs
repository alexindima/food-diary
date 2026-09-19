using FoodDiary.Modules.Meals.Contracts.Queries.ReadMealCount;
using FoodDiary.Modules.Hydration.Contracts.Queries.ReadHydrationDailyTotals;
using FoodDiary.Modules.Meals.Contracts.Queries.ReadMealNutritionStatistics;
using FoodDiary.Mediator;
using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Queries.ReadWaistEntries;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Queries.ReadWeightEntries;
using FoodDiary.Results;
using FoodDiary.Modules.Meals.Contracts.Models;
using FoodDiary.Modules.Meals.Contracts.Common;
using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Models;
using FoodDiary.Modules.WeeklyCheckIn.Application.Models;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Modules.WeeklyCheckIn.Application.Services;
using FoodDiary.Modules.Users.Contracts.Common;

namespace FoodDiary.Modules.WeeklyCheckIn.Application.Queries.GetWeeklyCheckIn;

public sealed class GetWeeklyCheckInQueryHandler(
    ISender sender,
    ICurrentUserAccessService currentUserAccessService,
    IUserWeeklyCheckInProfileReadService userProfileReadService,
    TimeProvider dateTimeProvider)
    : IQueryHandler<GetWeeklyCheckInQuery, Result<WeeklyCheckInModel>> {
    private static readonly DateOnly EarliestSupportedWeekStart = DateOnly.MinValue.AddDays(7);

    public async Task<Result<WeeklyCheckInModel>> Handle(
        GetWeeklyCheckInQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<WeeklyCheckInModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        Result<UserWeeklyCheckInProfileModel> profileResult = await userProfileReadService.GetWeeklyCheckInProfileAsync(userId, cancellationToken).ConfigureAwait(false);
        if (profileResult.IsFailure) {
            return Result.Failure<WeeklyCheckInModel>(profileResult.Error);
        }

        UserWeeklyCheckInProfileModel profile = profileResult.Value;
        DateTime today = dateTimeProvider.GetUtcNow().UtcDateTime.Date;
        DateTime currentWeekStart = StartOfWeek(today);
        if (query.WeekStart is { } requestedWeek && requestedWeek < EarliestSupportedWeekStart) {
            return Result.Failure<WeeklyCheckInModel>(Errors.Validation.Invalid(
                nameof(query.WeekStart),
                "The previous week is outside the supported date range."));
        }

        DateTime thisWeekStart = query.WeekStart is { } requestedWeekStart
            ? DateTime.SpecifyKind(requestedWeekStart.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc)
            : currentWeekStart;
        if (thisWeekStart.DayOfWeek != DayOfWeek.Monday) {
            return Result.Failure<WeeklyCheckInModel>(Errors.Validation.Invalid(
                nameof(query.WeekStart),
                "Week start must be a Monday."));
        }

        if (thisWeekStart > currentWeekStart) {
            return Result.Failure<WeeklyCheckInModel>(Errors.Validation.Invalid(
                nameof(query.WeekStart),
                "Future weeks are not available."));
        }

        DateTime thisWeekEnd = thisWeekStart == currentWeekStart ? today : thisWeekStart.AddDays(6);
        DateTime lastWeekStart = thisWeekStart.AddDays(-7);
        DateTime lastWeekEnd = thisWeekStart.AddDays(-1);

        Result<WeekSummaryModel> thisWeekSummaryResult = await LoadWeekSummaryAsync(userId, thisWeekStart, thisWeekEnd, cancellationToken).ConfigureAwait(false);
        if (thisWeekSummaryResult.IsFailure) {
            return Result.Failure<WeeklyCheckInModel>(thisWeekSummaryResult.Error);
        }

        Result<WeekSummaryModel> lastWeekSummaryResult = await LoadWeekSummaryAsync(userId, lastWeekStart, lastWeekEnd, cancellationToken).ConfigureAwait(false);
        if (lastWeekSummaryResult.IsFailure) {
            return Result.Failure<WeeklyCheckInModel>(lastWeekSummaryResult.Error);
        }

        WeekSummaryModel thisWeekSummary = thisWeekSummaryResult.Value;
        WeekSummaryModel lastWeekSummary = lastWeekSummaryResult.Value;
        WeekTrendModel trends = WeeklyCheckInCalculator.BuildTrends(thisWeekSummary, lastWeekSummary);
        IReadOnlyList<string> suggestions = WeeklyCheckInCalculator.GenerateSuggestions(thisWeekSummary, trends, profile.DailyCalorieTarget);

        return Result.Success(new WeeklyCheckInModel(thisWeekSummary, lastWeekSummary, trends, suggestions));
    }

    private static DateTime StartOfWeek(DateTime date) {
        int daysSinceMonday = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-daysSinceMonday);
    }
    private async Task<Result<WeekSummaryModel>> LoadWeekSummaryAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken) {
        Result<IReadOnlyList<MealNutritionStatisticsBucket>> nutritionResult = await sender.Send(new ReadMealNutritionStatisticsQuery(
            userId,
            dateFrom,
            dateTo.Date.AddDays(1).AddTicks(-10),
            QuantizationDays: 1),
            cancellationToken).ConfigureAwait(false);
        if (nutritionResult.IsFailure) {
            return Result.Failure<WeekSummaryModel>(nutritionResult.Error);
        }

        int mealCount = await sender.Send(new ReadMealCountQuery(
            userId,
            new MealQueryFilters(DateFrom: dateFrom, DateTo: dateTo)),
            cancellationToken).ConfigureAwait(false);

        IReadOnlyList<WeightEntryModel> weights = await sender.Send(new ReadWeightEntriesQuery(UserId: userId, DateFrom: dateFrom, DateTo: dateTo, Limit: null, Descending: false), cancellationToken)
            .ConfigureAwait(false);
        IReadOnlyList<WaistEntryModel> waists = await sender.Send(new ReadWaistEntriesQuery(UserId: userId, DateFrom: dateFrom, DateTo: dateTo, Limit: null, Descending: false), cancellationToken)
            .ConfigureAwait(false);
        IReadOnlyList<(DateTime Date, int TotalMl)> hydration = await sender.Send(new ReadHydrationDailyTotalsQuery(userId, dateFrom, dateTo), cancellationToken)
            .ConfigureAwait(false);

        return Result.Success(WeeklyCheckInCalculator.BuildSummary(
            nutritionResult.Value,
            mealCount,
            weights,
            waists,
            hydration,
            daysInPeriod: (dateTo.Date - dateFrom.Date).Days + 1));
    }
}
