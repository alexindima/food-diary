using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Application.WeeklyCheckIn.Models;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Tracking;

namespace FoodDiary.Application.WeeklyCheckIn.Services;

public static class WeeklyCheckInCalculator {
    public static WeekSummaryModel BuildSummary(
        IReadOnlyList<Meal> meals,
        IReadOnlyList<WeightEntry> weights,
        IReadOnlyList<WaistEntry> waists,
        IReadOnlyList<(DateTime Date, int TotalMl)> hydration,
        int daysInPeriod) =>
        BuildSummary(
            [.. meals
                .GroupBy(static meal => meal.Date.Date)
                .Select(static group => new DashboardStatisticsBucketReadModel(
                    group.Key,
                    group.Key,
                    group.Sum(meal => meal.TotalCalories),
                    group.Sum(meal => meal.TotalProteins),
                    group.Sum(meal => meal.TotalFats),
                    group.Sum(meal => meal.TotalCarbs),
                    AverageFiber: 0,
                    group.Sum(meal => meal.TotalProteins),
                    group.Sum(meal => meal.TotalFats),
                    group.Sum(meal => meal.TotalCarbs),
                    group.Sum(meal => meal.TotalFiber)))],
            meals.Count,
            weights,
            waists,
            hydration,
            daysInPeriod);

    public static WeekSummaryModel BuildSummary(
        IReadOnlyList<DashboardStatisticsBucketReadModel> nutritionBuckets,
        int mealsLogged,
        IReadOnlyList<WeightEntry> weights,
        IReadOnlyList<WaistEntry> waists,
        IReadOnlyList<(DateTime Date, int TotalMl)> hydration,
        int daysInPeriod) {
        double totalCalories = nutritionBuckets.Sum(bucket => bucket.TotalCalories);
        double avgDaily = daysInPeriod > 0 ? totalCalories / daysInPeriod : 0;
        double avgProteins = daysInPeriod > 0 ? nutritionBuckets.Sum(bucket => bucket.TotalProteins) / daysInPeriod : 0;
        double avgFats = daysInPeriod > 0 ? nutritionBuckets.Sum(bucket => bucket.TotalFats) / daysInPeriod : 0;
        double avgCarbs = daysInPeriod > 0 ? nutritionBuckets.Sum(bucket => bucket.TotalCarbs) / daysInPeriod : 0;
        int daysLogged = nutritionBuckets.Count(static bucket => bucket.TotalCalories > 0);

        var sortedWeights = weights.OrderBy(w => w.Date).ToList();
        double? weightStart = sortedWeights.FirstOrDefault()?.Weight;
        double? weightEnd = sortedWeights.LastOrDefault()?.Weight;

        var sortedWaists = waists.OrderBy(w => w.Date).ToList();
        double? waistStart = sortedWaists.FirstOrDefault()?.Circumference;
        double? waistEnd = sortedWaists.LastOrDefault()?.Circumference;

        int totalHydration = hydration.Sum(h => h.TotalMl);
        int avgHydration = daysInPeriod > 0 ? totalHydration / daysInPeriod : 0;

        return new WeekSummaryModel(
            Math.Round(totalCalories, 1, MidpointRounding.ToEven),
            Math.Round(avgDaily, 1, MidpointRounding.ToEven),
            Math.Round(avgProteins, 1, MidpointRounding.ToEven),
            Math.Round(avgFats, 1, MidpointRounding.ToEven),
            Math.Round(avgCarbs, 1, MidpointRounding.ToEven),
            mealsLogged,
            daysLogged,
            weightStart,
            weightEnd,
            waistStart,
            waistEnd,
            totalHydration,
            avgHydration);
    }

    public static WeekTrendModel BuildTrends(WeekSummaryModel thisWeek, WeekSummaryModel lastWeek) {
        return new WeekTrendModel(
            CalorieChange: Math.Round(thisWeek.AvgDailyCalories - lastWeek.AvgDailyCalories, 1, MidpointRounding.ToEven),
            ProteinChange: Math.Round(thisWeek.AvgProteins - lastWeek.AvgProteins, 1, MidpointRounding.ToEven),
            FatChange: Math.Round(thisWeek.AvgFats - lastWeek.AvgFats, 1, MidpointRounding.ToEven),
            CarbChange: Math.Round(thisWeek.AvgCarbs - lastWeek.AvgCarbs, 1, MidpointRounding.ToEven),
            WeightChange: thisWeek.WeightEnd.HasValue && lastWeek.WeightEnd.HasValue
                ? Math.Round(thisWeek.WeightEnd.Value - lastWeek.WeightEnd.Value, 2, MidpointRounding.ToEven) : null,
            WaistChange: thisWeek.WaistEnd.HasValue && lastWeek.WaistEnd.HasValue
                ? Math.Round(thisWeek.WaistEnd.Value - lastWeek.WaistEnd.Value, 2, MidpointRounding.ToEven) : null,
            HydrationChange: thisWeek.AvgDailyHydrationMl - lastWeek.AvgDailyHydrationMl,
            MealsLoggedChange: thisWeek.MealsLogged - lastWeek.MealsLogged);
    }

    public static IReadOnlyList<string> GenerateSuggestions(
        WeekSummaryModel thisWeek,
        WeekTrendModel trends,
        double? dailyCalorieTarget) {
        var suggestions = new List<string>();

        if (thisWeek.DaysLogged < 5) {
            suggestions.Add("suggestion.log_more_days");
        }

        if (dailyCalorieTarget is > 0) {
            switch (thisWeek.AvgDailyCalories / dailyCalorieTarget.Value) {
                case > 1.15:
                    suggestions.Add("suggestion.over_calorie_goal");
                    break;
                case < 0.85 when thisWeek.DaysLogged >= 3:
                    suggestions.Add("suggestion.under_calorie_goal");
                    break;
                case >= 0.85 and <= 1.15 when thisWeek.DaysLogged >= 5:
                    suggestions.Add("suggestion.on_track");
                    break;
            }
        }

        if (trends.WeightChange.HasValue && trends.WeightChange > 0.5) {
            suggestions.Add("suggestion.weight_increasing");
        } else if (trends.WeightChange.HasValue && trends.WeightChange < -0.5) {
            suggestions.Add("suggestion.weight_decreasing");
        }

        if (thisWeek.AvgDailyHydrationMl < 1500 && thisWeek.AvgDailyHydrationMl > 0) {
            suggestions.Add("suggestion.drink_more_water");
        }

        if (thisWeek.AvgProteins > 0 && thisWeek.AvgProteins < 50) {
            suggestions.Add("suggestion.low_protein");
        }

        if (suggestions.Count == 0) {
            suggestions.Add("suggestion.keep_going");
        }

        return suggestions;
    }
}
