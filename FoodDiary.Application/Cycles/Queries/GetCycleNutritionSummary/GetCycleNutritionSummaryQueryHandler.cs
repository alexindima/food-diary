using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Cycles.Common;
using FoodDiary.Application.Abstractions.Dashboard.Common;
using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Time;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Cycles.Queries.GetCycleNutritionSummary;

public sealed class GetCycleNutritionSummaryQueryHandler(
    ICycleReadRepository cycleRepository,
    IDashboardStatisticsReadService statisticsReadService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetCycleNutritionSummaryQuery, Result<CycleNutritionSummaryModel?>> {
    private const int MaxSummaryRangeDays = 366;
    private const int MinComparisonDaysPerGroup = 2;

    public async Task<Result<CycleNutritionSummaryModel?>> Handle(
        GetCycleNutritionSummaryQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<CycleNutritionSummaryModel?>(userIdResult.Error);
        }

        DateTime normalizedFrom = UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(query.DateFrom);
        DateTime normalizedTo = UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(query.DateTo);
        if (normalizedFrom > normalizedTo) {
            return Result.Failure<CycleNutritionSummaryModel?>(
                Errors.Validation.Invalid(nameof(query.DateFrom), "DateFrom must be less than or equal to DateTo."));
        }

        if ((normalizedTo - normalizedFrom).TotalDays > MaxSummaryRangeDays) {
            return Result.Failure<CycleNutritionSummaryModel?>(
                Errors.Validation.Invalid(nameof(query.DateTo), "Summary range must not exceed one year."));
        }

        UserId userId = userIdResult.Value;
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<CycleNutritionSummaryModel?>(accessError);
        }

        CycleProfile? profile = await cycleRepository.GetCurrentAsync(
            userId,
            includeDetails: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (profile is null) {
            return Result.Success<CycleNutritionSummaryModel?>(value: null);
        }

        Result<IReadOnlyList<DashboardStatisticsBucketReadModel>> nutritionResult = await statisticsReadService.GetStatisticsAsync(
            userId,
            normalizedFrom.Date,
            normalizedTo.Date.AddDays(1).AddTicks(-1),
            quantizationDays: 1,
            cancellationToken).ConfigureAwait(false);
        if (nutritionResult.IsFailure) {
            return Result.Failure<CycleNutritionSummaryModel?>(nutritionResult.Error);
        }

        return Result.Success<CycleNutritionSummaryModel?>(BuildSummary(profile, nutritionResult.Value, normalizedFrom, normalizedTo));
    }

    private static CycleNutritionSummaryModel BuildSummary(
        CycleProfile profile,
        IReadOnlyCollection<DashboardStatisticsBucketReadModel> nutritionBuckets,
        DateTime dateFrom,
        DateTime dateTo) {
        var nutritionByDate = nutritionBuckets
            .Where(static bucket => bucket.TotalCalories > 0 || bucket.TotalFiber > 0)
            .ToDictionary(static bucket => bucket.DateFrom.Date);
        List<CycleNutritionDay> days = BuildCycleDays(profile, nutritionByDate, dateFrom.Date, dateTo.Date);
        var bleedingDays = days.Where(day => day.IsBleeding && day.HasMeals).ToList();
        var nonBleedingDays = days.Where(day => !day.IsBleeding && day.HasMeals).ToList();

        return new CycleNutritionSummaryModel(
            dateFrom,
            dateTo,
            days.Count,
            days.Count(day => day.HasMeals),
            days.Count(day => day.IsBleeding),
            Average(bleedingDays, day => day.Calories),
            Average(nonBleedingDays, day => day.Calories),
            Average(bleedingDays, day => day.Fiber),
            Average(nonBleedingDays, day => day.Fiber),
            Average(days.Where(day => day.HasMeals && day.PainImpact.HasValue), day => day.PainImpact ?? 0),
            bleedingDays.Count >= MinComparisonDaysPerGroup && nonBleedingDays.Count >= MinComparisonDaysPerGroup);
    }

    private static List<CycleNutritionDay> BuildCycleDays(
        CycleProfile profile,
        IReadOnlyDictionary<DateTime, DashboardStatisticsBucketReadModel> nutritionByDate,
        DateTime dateFrom,
        DateTime dateTo) {
        DateTime[] logDates = [
            .. profile.BleedingEntries.Select(entry => entry.Date.Date),
            .. profile.SymptomEntries.Select(entry => entry.Date.Date),
            .. profile.FertilitySignals.Select(signal => signal.Date.Date),
        ];

        return [
            .. logDates
            .Where(date => date >= dateFrom && date <= dateTo)
            .Distinct()
            .Order()
            .Select(date => BuildDay(profile, nutritionByDate, date)),
        ];
    }

    private static CycleNutritionDay BuildDay(
        CycleProfile profile,
        IReadOnlyDictionary<DateTime, DashboardStatisticsBucketReadModel> nutritionByDate,
        DateTime date) {
        nutritionByDate.TryGetValue(date, out DashboardStatisticsBucketReadModel? nutrition);
        IReadOnlyCollection<BleedingEntry> bleedingEntries = [
            .. profile.BleedingEntries
            .Where(entry => entry.Date.Date == date),
        ];

        return new CycleNutritionDay(
            date,
            nutrition is not null,
            bleedingEntries.Any(entry => entry.Type == BleedingType.Bleeding),
            nutrition?.TotalCalories ?? 0,
            nutrition?.TotalFiber ?? 0,
            bleedingEntries.Select(entry => entry.PainImpact).FirstOrDefault(value => value.HasValue));
    }

    private static double Average(IEnumerable<CycleNutritionDay> days, Func<CycleNutritionDay, double> selector) {
        var items = days.ToList();
        return items.Count == 0
            ? 0
            : Math.Round(items.Average(selector), 2, MidpointRounding.ToEven);
    }

    private sealed record CycleNutritionDay(
        DateTime Date,
        bool HasMeals,
        bool IsBleeding,
        double Calories,
        double Fiber,
        int? PainImpact);
}
