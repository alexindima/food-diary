using FoodDiary.Application.Gamification.Models;
using FoodDiary.Domain.Entities.Meals;

namespace FoodDiary.Application.Gamification.Services;

public static class GamificationCalculator {
    private static readonly (string Key, string Category, int Threshold)[] BadgeDefinitions = [
        ("streak_3", "streak", 3),
        ("streak_7", "streak", 7),
        ("streak_14", "streak", 14),
        ("streak_30", "streak", 30),
        ("streak_60", "streak", 60),
        ("streak_100", "streak", 100),
        ("meals_10", "meals", 10),
        ("meals_50", "meals", 50),
        ("meals_100", "meals", 100),
        ("meals_500", "meals", 500),
    ];

    public static (int CurrentStreak, int LongestStreak) CalculateStreaks(IReadOnlyList<DateTime> sortedDatesDesc, DateTime today) {
        if (sortedDatesDesc.Count == 0) {
            return (0, 0);
        }

        var currentStreak = 0;
        var longestStreak = 0;
        var runStreak = 1;

        var firstDate = sortedDatesDesc[0].Date;
        var isCurrentRun = firstDate == today || firstDate == today.AddDays(-1);

        if (isCurrentRun) {
            currentStreak = 1;
        }

        for (var i = 1; i < sortedDatesDesc.Count; i++) {
            var prev = sortedDatesDesc[i - 1].Date;
            var curr = sortedDatesDesc[i].Date;
            var gap = (prev - curr).Days;

            if (gap == 1) {
                runStreak++;
                if (isCurrentRun) {
                    currentStreak = runStreak;
                }
            } else if (gap > 1) {
                longestStreak = Math.Max(longestStreak, runStreak);
                runStreak = 1;
                isCurrentRun = false;
            }
        }

        longestStreak = Math.Max(longestStreak, runStreak);
        return (currentStreak, longestStreak);
    }

    public static IReadOnlyList<BadgeModel> CalculateBadges(int longestStreak, int totalMeals) {
        return BadgeDefinitions.Select(b => new BadgeModel(
            b.Key,
            b.Category,
            b.Threshold,
            b.Category == "streak" ? longestStreak >= b.Threshold : totalMeals >= b.Threshold
        )).ToList();
    }

    public static int CalculateHealthScore(
        int currentStreak,
        double weeklyAdherence,
        int totalMeals) {
        var streakScore = Math.Min(currentStreak, 30) / 30.0 * 40;
        var adherenceScore = weeklyAdherence * 40;
        var activityScore = Math.Min(totalMeals, 100) / 100.0 * 20;

        return (int)Math.Round(streakScore + adherenceScore + activityScore);
    }

    public static double CalculateWeeklyAdherence(
        IReadOnlyList<Meal> weekMeals,
        double? dailyCalorieTarget,
        DateTime today) {
        if (dailyCalorieTarget is null or <= 0) {
            return weekMeals.Count > 0 ? 1.0 : 0.0;
        }

        var weekStart = today.AddDays(-6);
        var daysInRange = Math.Min(7, (int)(today - weekStart).TotalDays + 1);
        var metDays = 0;

        for (var d = 0; d < daysInRange; d++) {
            var date = weekStart.AddDays(d);
            var dayCalories = weekMeals
                .Where(m => m.Date.Date == date)
                .Sum(m => m.TotalCalories);

            if (dayCalories > 0) {
                var ratio = dayCalories / dailyCalorieTarget.Value;
                if (ratio >= 0.8 && ratio <= 1.2) {
                    metDays++;
                }
            }
        }

        return (double)metDays / daysInRange;
    }
}
