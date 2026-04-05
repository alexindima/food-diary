using FoodDiary.Application.Tdee.Models;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Tracking;

namespace FoodDiary.Application.Tdee.Services;

public static class TdeeCalculator {
    private const double KcalPerKgBodyWeight = 7700.0;
    private const int MinDaysForAdaptive = 14;
    private const int MinWeightEntries = 3;
    private const double MaxReasonableTdee = 6000.0;
    private const double MinReasonableTdee = 800.0;

    public static AdaptiveTdeeResult CalculateAdaptive(
        IReadOnlyList<WeightEntry> weightEntries,
        IReadOnlyList<Meal> meals,
        int periodDays) {
        var sortedWeights = weightEntries.OrderBy(w => w.Date).ToList();
        if (sortedWeights.Count < MinWeightEntries || periodDays < MinDaysForAdaptive) {
            return AdaptiveTdeeResult.Insufficient;
        }

        var dailyCalories = CalculateDailyCalories(meals);
        var daysWithCalories = dailyCalories.Count;
        if (daysWithCalories < MinDaysForAdaptive) {
            return AdaptiveTdeeResult.Insufficient;
        }

        var avgDailyIntake = dailyCalories.Values.Average();

        var smoothedStart = GetSmoothedWeight(sortedWeights, takeLast: false, count: 3);
        var smoothedEnd = GetSmoothedWeight(sortedWeights, takeLast: true, count: 3);

        var firstDate = sortedWeights[0].Date;
        var lastDate = sortedWeights[^1].Date;
        var actualDays = (lastDate - firstDate).TotalDays;
        if (actualDays < MinDaysForAdaptive) {
            return AdaptiveTdeeResult.Insufficient;
        }

        var weightChange = smoothedEnd - smoothedStart;
        var weightChangePerDay = weightChange / actualDays;
        var caloriesFromWeightChange = weightChangePerDay * KcalPerKgBodyWeight;

        // TDEE = average intake - caloric surplus from weight change
        // If losing weight (negative change), TDEE > intake
        // If gaining weight (positive change), TDEE < intake
        var adaptiveTdee = avgDailyIntake - caloriesFromWeightChange;

        if (adaptiveTdee < MinReasonableTdee || adaptiveTdee > MaxReasonableTdee) {
            return AdaptiveTdeeResult.Insufficient;
        }

        var confidence = DetermineConfidence(sortedWeights.Count, daysWithCalories, actualDays);
        var weightTrendPerWeek = Math.Round(weightChangePerDay * 7, 2);

        return new AdaptiveTdeeResult(
            Math.Round(adaptiveTdee, 0),
            confidence,
            daysWithCalories,
            weightTrendPerWeek);
    }

    public static double? SuggestCalorieTarget(
        double adaptiveTdee,
        double? currentWeight,
        double? desiredWeight) {
        if (currentWeight is null || desiredWeight is null) {
            return Math.Round(adaptiveTdee, 0);
        }

        var deficit = currentWeight > desiredWeight
            ? -500.0  // lose ~0.45 kg/week
            : currentWeight < desiredWeight
                ? 300.0   // gain ~0.27 kg/week (lean bulk)
                : 0.0;    // maintain

        var target = adaptiveTdee + deficit;
        return Math.Round(Math.Max(target, 1200.0), 0);
    }

    public static string? GetGoalAdjustmentHint(
        double? adaptiveTdee,
        double? currentTarget,
        double? currentWeight,
        double? desiredWeight) {
        if (adaptiveTdee is null || currentTarget is null || currentTarget <= 0) {
            return null;
        }

        var diff = currentTarget.Value - adaptiveTdee.Value;
        var isLosing = currentWeight > desiredWeight;
        var isGaining = currentWeight < desiredWeight;

        return (diff, isLosing, isGaining) switch {
            ( < -700, true, _) => "hint.deficit_too_aggressive",
            ( < -300, true, _) => "hint.deficit_moderate",
            ( >= -300 and <= 300, _, _) when !isLosing && !isGaining => "hint.maintenance_on_track",
            ( >= -300 and <= 0, true, _) => "hint.deficit_mild",
            ( > 0, true, _) => "hint.surplus_while_losing_goal",
            ( > 500, _, true) => "hint.surplus_too_aggressive",
            ( >= 100 and <= 500, _, true) => "hint.surplus_moderate",
            _ => "hint.review_goals"
        };
    }

    private static Dictionary<DateTime, double> CalculateDailyCalories(IReadOnlyList<Meal> meals) {
        var daily = new Dictionary<DateTime, double>();
        foreach (var meal in meals) {
            var date = meal.Date.Date;
            if (!daily.TryGetValue(date, out var total)) {
                total = 0;
            }

            daily[date] = total + meal.TotalCalories;
        }

        return daily;
    }

    private static double GetSmoothedWeight(
        IReadOnlyList<WeightEntry> sorted,
        bool takeLast,
        int count) {
        var entries = takeLast
            ? sorted.TakeLast(count)
            : sorted.Take(count);
        return entries.Average(w => w.Weight);
    }

    private static TdeeConfidence DetermineConfidence(
        int weightEntryCount,
        int daysWithCalories,
        double actualDays) {
        if (weightEntryCount >= 10 && daysWithCalories >= 25 && actualDays >= 28) {
            return TdeeConfidence.High;
        }

        if (weightEntryCount >= 5 && daysWithCalories >= 18 && actualDays >= 21) {
            return TdeeConfidence.Medium;
        }

        return TdeeConfidence.Low;
    }
}

public sealed record AdaptiveTdeeResult(
    double? AdaptiveTdee,
    TdeeConfidence Confidence,
    int DataDaysUsed,
    double? WeightTrendPerWeek) {
    public static readonly AdaptiveTdeeResult Insufficient = new(null, TdeeConfidence.None, 0, null);

    public bool HasData => AdaptiveTdee.HasValue;
}
