using FoodDiary.Modules.Gamification.Domain.Contracts.Enums;

namespace FoodDiary.Modules.Gamification.Domain.ValueObjects;

public sealed record AchievementTarget {
    public AchievementMetric Metric { get; }
    public int Threshold { get; }

    private AchievementTarget(AchievementMetric metric, int threshold) {
        Metric = metric;
        Threshold = threshold;
    }

    public static AchievementTarget FromFields(AchievementMetric metric, int threshold) {
        if (!Enum.IsDefined(metric)) {
            throw new ArgumentOutOfRangeException(nameof(metric));
        }
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(threshold);
        return new AchievementTarget(metric, threshold);
    }

    public static AchievementTarget StreakDays(int days) => FromFields(AchievementMetric.LongestStreak, days);
    public static AchievementTarget MealCount(int count) => FromFields(AchievementMetric.TotalMeals, count);
    public static AchievementTarget ArticleCount(int count) => FromFields(AchievementMetric.TotalAcademyArticlesRead, count);

    internal static AchievementTarget FromStoredFields(AchievementMetric metric, int threshold) => new(metric, threshold);
}
