using FoodDiary.Modules.Meals.Contracts.Models;
using FoodDiary.Modules.Cycles.Application.Abstractions.Models;
using FoodDiary.Modules.Cycles.Contracts.Models;
using FoodDiary.Modules.Cycles.Domain.Contracts.Enums;

namespace FoodDiary.Modules.Cycles.Application.Queries.GetCycleNutritionSummary;

internal static class CycleNutritionSummaryCalculator {
    private const int MinimumComparableCycles = 3;
    private const string AlgorithmVersion = "nutrition-v2.0";

    public static CycleNutritionSummaryModel BuildSummary(
        CycleProfileReadModel profile,
        IReadOnlyCollection<MealNutritionStatisticsBucket> nutritionBuckets,
        DateOnly dateFrom,
        DateOnly dateTo) {
        IReadOnlyDictionary<DateOnly, MealNutritionStatisticsBucket> nutritionByDate = nutritionBuckets
            .Where(static bucket => bucket.TotalCalories > 0 || bucket.TotalFiber > 0)
            .GroupBy(static bucket => DateOnly.FromDateTime(bucket.DateFrom))
            .ToDictionary(static group => group.Key, static group => group.Last());
        IReadOnlyList<CycleInterval> intervals = BuildCompletedIntervals(profile, dateFrom, dateTo);
        IReadOnlyList<CycleNutritionAggregate> aggregates = [
            .. intervals
                .Select(interval => BuildAggregate(profile, nutritionByDate, interval))
                .Where(static aggregate => aggregate.NutritionDays.Count > 0),
        ];
        IReadOnlyList<CycleNutritionAggregate> comparable = [
            .. aggregates.Where(static aggregate => aggregate.BleedingNutritionDays.Count > 0 && aggregate.NonBleedingNutritionDays.Count > 0),
        ];
        CycleNutritionDay[] nutritionDays = [.. aggregates.SelectMany(static aggregate => aggregate.NutritionDays)];
        int completedCycles = intervals.Count;
        bool hasEnoughData = comparable.Count >= MinimumComparableCycles;
        string sufficiency = (hasEnoughData, completedCycles) switch {
            (true, _) => "Established",
            (false, > 0) => "Limited",
            _ => "Insufficient",
        };
        IReadOnlyCollection<string> reasonCodes = hasEnoughData
            ? ["per_cycle_comparison_available", "association_not_causation"]
            : ["at_least_three_comparable_cycles_required", "association_not_causation"];

        return new CycleNutritionSummaryModel(
            dateFrom,
            dateTo,
            nutritionDays.Length,
            nutritionDays.Length,
            intervals.SelectMany(interval => profile.BleedingEntries
                .Where(entry => entry.Type == BleedingType.Bleeding && interval.Contains(entry.Date))
                .Select(entry => entry.Date)).Distinct().Count(),
            Average(comparable, static aggregate => aggregate.AverageBleedingCalories),
            Average(comparable, static aggregate => aggregate.AverageNonBleedingCalories),
            Average(comparable, static aggregate => aggregate.AverageBleedingFiber),
            Average(comparable, static aggregate => aggregate.AverageNonBleedingFiber),
            Average(nutritionDays.Where(static day => day.PainImpact.HasValue), static day => day.PainImpact ?? 0),
            hasEnoughData,
            ConsentRequired: false,
            completedCycles,
            comparable.Count,
            sufficiency,
            reasonCodes,
            AlgorithmVersion);
    }

    private static IReadOnlyList<CycleInterval> BuildCompletedIntervals(
        CycleProfileReadModel profile,
        DateOnly dateFrom,
        DateOnly dateTo) {
        DateOnly[] excludedStarts = [
            .. (profile.MenstrualEpisodes ?? [])
                .Where(static episode => episode.Status == MenstrualEpisodeStatus.Confirmed && episode.ExcludedFromPredictions)
                .Select(static episode => episode.StartDate),
        ];
        DateOnly[] persistedStarts = [
            .. (profile.MenstrualEpisodes ?? [])
                .Where(static episode => !episode.ExcludedFromPredictions)
                .Select(static episode => episode.StartDate),
        ];
        DateOnly[] inferredStarts = BuildInferredStarts(
            profile.BleedingEntries
                .Where(static entry => entry.Type == BleedingType.Bleeding)
                .Select(static entry => entry.Date));
        DateOnly[] starts = [
            .. persistedStarts
                .Concat(inferredStarts.Where(inferred =>
                    !persistedStarts.Any(persisted => Math.Abs(persisted.DayNumber - inferred.DayNumber) <= 2)))
                .Where(start => !excludedStarts.Any(excluded => Math.Abs(excluded.DayNumber - start.DayNumber) <= 2))
                .Distinct()
                .Order(),
        ];

        var intervals = new List<CycleInterval>();
        for (int index = 0; index + 1 < starts.Length; index++) {
            DateOnly start = starts[index];
            DateOnly end = starts[index + 1].AddDays(-1);
            DateOnly boundedStart = start < dateFrom ? dateFrom : start;
            DateOnly boundedEnd = end > dateTo ? dateTo : end;
            if (boundedStart <= boundedEnd) {
                intervals.Add(new CycleInterval(boundedStart, boundedEnd));
            }
        }

        return intervals;
    }

    private static DateOnly[] BuildInferredStarts(IEnumerable<DateOnly> bleedingDates) {
        DateOnly[] dates = [.. bleedingDates.Distinct().Order()];
        if (dates.Length == 0) {
            return [];
        }

        var starts = new List<DateOnly> { dates[0] };
        for (int index = 1; index < dates.Length; index++) {
            if (dates[index].DayNumber - dates[index - 1].DayNumber > 2) {
                starts.Add(dates[index]);
            }
        }

        return [.. starts];
    }

    private static CycleNutritionAggregate BuildAggregate(
        CycleProfileReadModel profile,
        IReadOnlyDictionary<DateOnly, MealNutritionStatisticsBucket> nutritionByDate,
        CycleInterval interval) {
        CycleNutritionDay[] nutritionDays = [
            .. nutritionByDate
                .Where(item => interval.Contains(item.Key))
                .OrderBy(static item => item.Key)
                .Select(item => BuildDay(profile, item.Key, item.Value)),
        ];
        CycleNutritionDay[] bleedingDays = [.. nutritionDays.Where(static day => day.IsBleeding)];
        CycleNutritionDay[] nonBleedingDays = [.. nutritionDays.Where(static day => !day.IsBleeding)];

        return new CycleNutritionAggregate(
            nutritionDays,
            bleedingDays,
            nonBleedingDays,
            Average(bleedingDays, static day => day.Calories),
            Average(nonBleedingDays, static day => day.Calories),
            Average(bleedingDays, static day => day.Fiber),
            Average(nonBleedingDays, static day => day.Fiber));
    }

    private static CycleNutritionDay BuildDay(
        CycleProfileReadModel profile,
        DateOnly date,
        MealNutritionStatisticsBucket nutrition) {
        IReadOnlyCollection<BleedingEntryReadModel> bleedingEntries = [
            .. profile.BleedingEntries.Where(entry => entry.Date == date),
        ];

        return new CycleNutritionDay(
            date,
            bleedingEntries.Any(entry => entry.Type == BleedingType.Bleeding),
            nutrition.TotalCalories,
            nutrition.TotalFiber,
            bleedingEntries.Select(entry => entry.PainImpact).FirstOrDefault(value => value.HasValue));
    }

    public static CycleNutritionSummaryModel CreateConsentRequiredSummary(DateOnly dateFrom, DateOnly dateTo) =>
        new(
            dateFrom,
            dateTo,
            LoggedCycleDays: 0,
            DaysWithMeals: 0,
            BleedingDays: 0,
            AverageCaloriesOnBleedingDays: 0,
            AverageCaloriesOnNonBleedingCycleDays: 0,
            AverageFiberOnBleedingDays: 0,
            AverageFiberOnNonBleedingCycleDays: 0,
            AveragePainImpactOnDaysWithMeals: 0,
            HasEnoughNutritionData: false,
            ConsentRequired: true,
            CompletedCyclesAnalyzed: 0,
            ComparableCycles: 0,
            DataSufficiency: "Unavailable",
            ReasonCodes: ["nutrition_consent_required"],
            AlgorithmVersion);

    private static double Average<T>(IEnumerable<T> items, Func<T, double> selector) {
        T[] values = [.. items];
        return values.Length == 0
            ? 0
            : Math.Round(values.Average(selector), 2, MidpointRounding.ToEven);
    }

    private sealed record CycleInterval(DateOnly Start, DateOnly End) {
        public bool Contains(DateOnly date) => date >= Start && date <= End;
    }

    private sealed record CycleNutritionDay(
        DateOnly Date,
        bool IsBleeding,
        double Calories,
        double Fiber,
        int? PainImpact);

    private sealed record CycleNutritionAggregate(
        IReadOnlyList<CycleNutritionDay> NutritionDays,
        IReadOnlyList<CycleNutritionDay> BleedingNutritionDays,
        IReadOnlyList<CycleNutritionDay> NonBleedingNutritionDays,
        double AverageBleedingCalories,
        double AverageNonBleedingCalories,
        double AverageBleedingFiber,
        double AverageNonBleedingFiber);
}
