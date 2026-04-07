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
    private const double EmaAlpha = 0.1;

    public static AdaptiveTdeeResult CalculateAdaptive(
        IReadOnlyList<WeightEntry> weightEntries,
        IReadOnlyList<Meal> meals,
        int periodDays,
        IReadOnlyList<ExerciseEntry>? exerciseEntries = null) {
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
        var avgDailyExercise = CalculateAvgDailyExercise(exerciseEntries, dailyCalories.Count);

        var smoothedStart = GetEmaWeight(sortedWeights, fromStart: true);
        var smoothedEnd = GetEmaWeight(sortedWeights, fromStart: false);

        var firstDate = sortedWeights[0].Date;
        var lastDate = sortedWeights[^1].Date;
        var actualDays = (lastDate - firstDate).TotalDays;
        if (actualDays < MinDaysForAdaptive) {
            return AdaptiveTdeeResult.Insufficient;
        }

        var weightChange = smoothedEnd - smoothedStart;
        var weightChangePerDay = weightChange / actualDays;
        var caloriesFromWeightChange = weightChangePerDay * KcalPerKgBodyWeight;

        // TDEE = average food intake + average exercise burn - caloric surplus from weight change
        var adaptiveTdee = avgDailyIntake + avgDailyExercise - caloriesFromWeightChange;

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

    private static double CalculateAvgDailyExercise(
        IReadOnlyList<ExerciseEntry>? exerciseEntries,
        int daysWithCalories) {
        if (exerciseEntries is null || exerciseEntries.Count == 0 || daysWithCalories <= 0) {
            return 0;
        }

        var totalBurned = exerciseEntries.Sum(e => e.CaloriesBurned);
        return totalBurned / daysWithCalories;
    }

    /// <summary>
    /// Exponential Moving Average weight smoothing.
    /// When fromStart=true, runs EMA forward and returns the value at the midpoint.
    /// When fromStart=false, runs EMA backward (from end) and returns the value at the midpoint.
    /// This gives a smoothed estimate of weight at the beginning and end of the period.
    /// </summary>
    private static double GetEmaWeight(IReadOnlyList<WeightEntry> sorted, bool fromStart) {
        if (sorted.Count <= 3) {
            var entries = fromStart ? sorted.Take(sorted.Count) : sorted.TakeLast(sorted.Count);
            return entries.Average(w => w.Weight);
        }

        var half = sorted.Count / 2;
        if (fromStart) {
            var ema = sorted[0].Weight;
            for (var i = 1; i <= half; i++) {
                ema = EmaAlpha * sorted[i].Weight + (1 - EmaAlpha) * ema;
            }

            return ema;
        } else {
            var ema = sorted[^1].Weight;
            for (var i = sorted.Count - 2; i >= sorted.Count - 1 - half; i--) {
                ema = EmaAlpha * sorted[i].Weight + (1 - EmaAlpha) * ema;
            }

            return ema;
        }
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
