using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Cycles.Common;
using FoodDiary.Application.Abstractions.Cycles.Models;
using FoodDiary.Application.Abstractions.Dashboard.Common;
using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Application.Cycles.Common;
using FoodDiary.Application.Cycles.Mappings;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Cycles.Services;

public sealed class CycleReadService(
    ICycleReadRepository cycleRepository,
    IDashboardStatisticsReadService statisticsReadService)
    : ICycleReadService {
    private const int MinComparisonDaysPerGroup = 2;

    public async Task<CycleModel?> GetCurrentAsync(
        UserId userId,
        CancellationToken cancellationToken) {
        CycleProfileReadModel? profile = await GetCurrentProfileAsync(userId, cancellationToken).ConfigureAwait(false);
        if (profile is null) {
            return null;
        }

        CyclePredictionsModel predictions = CyclePredictionService.CalculatePredictions(profile);
        return profile.ToModel(predictions);
    }

    public async Task<Result<CycleNutritionSummaryModel?>> GetNutritionSummaryAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken) {
        CycleProfileReadModel? profile = await GetCurrentProfileAsync(userId, cancellationToken).ConfigureAwait(false);
        if (profile is null) {
            return Result.Success<CycleNutritionSummaryModel?>(value: null);
        }

        Result<IReadOnlyList<DashboardStatisticsBucketReadModel>> nutritionResult = await statisticsReadService.GetStatisticsAsync(
            userId,
            dateFrom.Date,
            dateTo.Date.AddDays(1).AddTicks(-1),
            quantizationDays: 1,
            cancellationToken).ConfigureAwait(false);
        if (nutritionResult.IsFailure) {
            return Result.Failure<CycleNutritionSummaryModel?>(nutritionResult.Error);
        }

        return Result.Success<CycleNutritionSummaryModel?>(BuildSummary(profile, nutritionResult.Value, dateFrom, dateTo));
    }

    private Task<CycleProfileReadModel?> GetCurrentProfileAsync(
        UserId userId,
        CancellationToken cancellationToken) =>
        cycleRepository.GetCurrentReadModelAsync(userId, cancellationToken);

    private static CycleNutritionSummaryModel BuildSummary(
        CycleProfileReadModel profile,
        IReadOnlyCollection<DashboardStatisticsBucketReadModel> nutritionBuckets,
        DateTime dateFrom,
        DateTime dateTo) {
        var nutritionByDate = nutritionBuckets
            .Where(static bucket => bucket.TotalCalories > 0 || bucket.TotalFiber > 0)
            .ToDictionary(static bucket => bucket.DateFrom.Date);
        List<CycleNutritionDay> days = BuildCycleDays(profile, nutritionByDate, dateFrom.Date, dateTo.Date);
        var bleedingDays = days.Where(static day => day.IsBleeding && day.HasMeals).ToList();
        var nonBleedingDays = days.Where(static day => !day.IsBleeding && day.HasMeals).ToList();

        return new CycleNutritionSummaryModel(
            dateFrom,
            dateTo,
            days.Count,
            days.Count(static day => day.HasMeals),
            days.Count(static day => day.IsBleeding),
            Average(bleedingDays, static day => day.Calories),
            Average(nonBleedingDays, static day => day.Calories),
            Average(bleedingDays, static day => day.Fiber),
            Average(nonBleedingDays, static day => day.Fiber),
            Average(days.Where(static day => day.HasMeals && day.PainImpact.HasValue), static day => day.PainImpact ?? 0),
            bleedingDays.Count >= MinComparisonDaysPerGroup && nonBleedingDays.Count >= MinComparisonDaysPerGroup);
    }

    private static List<CycleNutritionDay> BuildCycleDays(
        CycleProfileReadModel profile,
        IReadOnlyDictionary<DateTime, DashboardStatisticsBucketReadModel> nutritionByDate,
        DateTime dateFrom,
        DateTime dateTo) {
        DateTime[] logDates = [
            .. profile.BleedingEntries.Select(static entry => entry.Date.Date),
            .. profile.SymptomEntries.Select(static entry => entry.Date.Date),
            .. profile.FertilitySignals.Select(static signal => signal.Date.Date),
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
        CycleProfileReadModel profile,
        IReadOnlyDictionary<DateTime, DashboardStatisticsBucketReadModel> nutritionByDate,
        DateTime date) {
        nutritionByDate.TryGetValue(date, out DashboardStatisticsBucketReadModel? nutrition);
        IReadOnlyCollection<BleedingEntryReadModel> bleedingEntries = [
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
