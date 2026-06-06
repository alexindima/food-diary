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

        int currentStreak = 0;
        int longestStreak = 0;
        int runStreak = 1;

        DateTime firstDate = sortedDatesDesc[0].Date;
        bool isCurrentRun = firstDate == today || firstDate == today.AddDays(-1);

        if (isCurrentRun) {
            currentStreak = 1;
        }

        for (int i = 1; i < sortedDatesDesc.Count; i++) {
            DateTime prev = sortedDatesDesc[i - 1].Date;
            DateTime curr = sortedDatesDesc[i].Date;
            switch ((prev - curr).Days) {
                case 1:
                    runStreak++;
                    if (isCurrentRun) {
                        currentStreak = runStreak;
                    }

                    break;

                case > 1:
                    longestStreak = Math.Max(longestStreak, runStreak);
                    runStreak = 1;
                    isCurrentRun = false;
                    break;
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
string.Equals(b.Category, "streak", StringComparison.Ordinal) ? longestStreak >= b.Threshold : totalMeals >= b.Threshold
        )).ToList();
    }

    public static int CalculateHealthScore(
        int currentStreak,
        double weeklyAdherence,
        int totalMeals) {
        double streakScore = Math.Min(currentStreak, 30) / 30.0 * 40;
        double adherenceScore = weeklyAdherence * 40;
        double activityScore = Math.Min(totalMeals, 100) / 100.0 * 20;

        return (int)Math.Round(streakScore + adherenceScore + activityScore, MidpointRounding.ToEven);
    }

    public static double CalculateWeeklyAdherence(
        IReadOnlyList<Meal> weekMeals,
        Func<DateTime, double?> getCalorieTarget,
        DateTime today) {
        DateTime weekStart = today.AddDays(-6);
        int daysInRange = Math.Min(7, (int)(today - weekStart).TotalDays + 1);
        int metDays = 0;
        int daysWithGoal = 0;

        for (int d = 0; d < daysInRange; d++) {
            DateTime date = weekStart.AddDays(d);
            double? target = getCalorieTarget(date);
            if (target is null or <= 0) {
                continue;
            }

            daysWithGoal++;
            double dayCalories = weekMeals
                .Where(m => m.Date.Date == date)
                .Sum(m => m.TotalCalories);

            if (!(dayCalories > 0)) {
                continue;
            }

            double ratio = dayCalories / target.Value;
            if (ratio is >= 0.8 and <= 1.2) {
                metDays++;
            }
        }

        if (daysWithGoal > 0) {
            return (double)metDays / daysWithGoal;
        }

        return weekMeals.Count > 0 ? 1.0 : 0.0;
    }
}
