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
        int daysInPeriod) {
        var totalCalories = meals.Sum(m => m.TotalCalories);
        var avgDaily = daysInPeriod > 0 ? totalCalories / daysInPeriod : 0;
        var avgProteins = daysInPeriod > 0 ? meals.Sum(m => m.TotalProteins) / daysInPeriod : 0;
        var avgFats = daysInPeriod > 0 ? meals.Sum(m => m.TotalFats) / daysInPeriod : 0;
        var avgCarbs = daysInPeriod > 0 ? meals.Sum(m => m.TotalCarbs) / daysInPeriod : 0;
        var mealsLogged = meals.Count;
        var daysLogged = meals.Select(m => m.Date.Date).Distinct().Count();

        var sortedWeights = weights.OrderBy(w => w.Date).ToList();
        var weightStart = sortedWeights.FirstOrDefault()?.Weight;
        var weightEnd = sortedWeights.LastOrDefault()?.Weight;

        var sortedWaists = waists.OrderBy(w => w.Date).ToList();
        var waistStart = sortedWaists.FirstOrDefault()?.Circumference;
        var waistEnd = sortedWaists.LastOrDefault()?.Circumference;

        var totalHydration = hydration.Sum(h => h.TotalMl);
        var avgHydration = daysInPeriod > 0 ? totalHydration / daysInPeriod : 0;

        return new WeekSummaryModel(
            Math.Round(totalCalories, 1),
            Math.Round(avgDaily, 1),
            Math.Round(avgProteins, 1),
            Math.Round(avgFats, 1),
            Math.Round(avgCarbs, 1),
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
            CalorieChange: Math.Round(thisWeek.AvgDailyCalories - lastWeek.AvgDailyCalories, 1),
            ProteinChange: Math.Round(thisWeek.AvgProteins - lastWeek.AvgProteins, 1),
            FatChange: Math.Round(thisWeek.AvgFats - lastWeek.AvgFats, 1),
            CarbChange: Math.Round(thisWeek.AvgCarbs - lastWeek.AvgCarbs, 1),
            WeightChange: thisWeek.WeightEnd.HasValue && lastWeek.WeightEnd.HasValue
                ? Math.Round(thisWeek.WeightEnd.Value - lastWeek.WeightEnd.Value, 2) : null,
            WaistChange: thisWeek.WaistEnd.HasValue && lastWeek.WaistEnd.HasValue
                ? Math.Round(thisWeek.WaistEnd.Value - lastWeek.WaistEnd.Value, 2) : null,
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

        if (dailyCalorieTarget.HasValue && dailyCalorieTarget > 0) {
            var ratio = thisWeek.AvgDailyCalories / dailyCalorieTarget.Value;
            if (ratio > 1.15) {
                suggestions.Add("suggestion.over_calorie_goal");
            } else if (ratio < 0.85 && thisWeek.DaysLogged >= 3) {
                suggestions.Add("suggestion.under_calorie_goal");
            } else if (ratio >= 0.85 && ratio <= 1.15 && thisWeek.DaysLogged >= 5) {
                suggestions.Add("suggestion.on_track");
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
