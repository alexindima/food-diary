using FoodDiary.Telegram.Bot.Operations;

namespace FoodDiary.Telegram.Bot.Tests;

[ExcludeFromCodeCoverage]
public sealed class BotStatisticsFormatterTests {
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Today_DisplaysGoalsAndTimeZone(bool russian) {
        var summary = new BotDiaryStatistics("Asia/Tbilisi", 1, 1, 1200, 60, 40, 150, 12, 500, 2, 1200, 2000,
            [new BotDiaryStatisticsDay(new DateOnly(2026, 9, 12), 1200, 60, 40, 150, 12, 500, 2, 2100)]);
        string text = BotStatisticsFormatter.Format(summary, russian).Replace('\u00a0', ' ').Replace('\u202f', ' ');
        Assert.Multiple(
            () => Assert.Contains(russian ? "1 200 / 2 100 ккал" : "1,200 / 2,100 kcal", text, StringComparison.Ordinal),
            () => Assert.Matches(@"57\s*%", text),
            () => Assert.Contains(russian ? "Осталось: 900 ккал" : "Remaining: 900 kcal", text, StringComparison.Ordinal),
            () => Assert.Contains("500 / 2000", text, StringComparison.Ordinal),
            () => Assert.Contains("Asia/Tbilisi", text, StringComparison.Ordinal),
            () => Assert.Contains(russian ? "Сегодня" : "Today", text, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(null, 600, "Calories: 600 kcal")]
    [InlineData(0, 600, "Calories: 600 kcal")]
    [InlineData(500, 600, "Over goal: 100 kcal")]
    [InlineData(500, 500, "Remaining: 0 kcal")]
    public void Today_HandlesMissingMetAndExceededCalorieGoals(int? goal, int calories, string expected) {
        var summary = new BotDiaryStatistics("UTC", 1, 1, calories, 0, 0, 0, 0, 0, 1, calories, DailyWaterGoalMl: null,
            [new BotDiaryStatisticsDay(new DateOnly(2026, 9, 12), calories, 0, 0, 0, 0, 0, 1, goal)]);
        string text = BotStatisticsFormatter.Format(summary, russian: false);
        Assert.Contains(expected, text, StringComparison.Ordinal);
        if (goal is null or 0) {
            Assert.DoesNotContain("%", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Remaining", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Week_UsesServerAverageAndMakesDenominatorExplicit() {
        BotDiaryStatisticsDay[] days = [.. Enumerable.Range(0, 7).Select(offset => new BotDiaryStatisticsDay(
            new DateOnly(2026, 9, 6).AddDays(offset), 0, 0, 0, 0, 0, 0, 0, CalorieGoal: null))];
        var summary = new BotDiaryStatistics("UTC", 7, 1, 700, 0, 0, 0, 0, 0, 1, 100, DailyWaterGoalMl: null, days);
        string text = BotStatisticsFormatter.Format(summary, russian: false);
        Assert.Multiple(
            () => Assert.Contains("Average over 7 calendar days: 100 kcal/day", text, StringComparison.Ordinal),
            () => Assert.Contains("Days with meals: 1/7", text, StringComparison.Ordinal),
            () => Assert.Contains("06.09.2026 – 12.09.2026", text, StringComparison.Ordinal));
    }

    [Fact]
    public void EmptyDay_IsExplicitAndInvalidResponseIsNotPresentedAsEmpty() {
        var summary = new BotDiaryStatistics("UTC", 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, DailyWaterGoalMl: null,
            [new BotDiaryStatisticsDay(new DateOnly(2026, 9, 12), 0, 0, 0, 0, 0, 0, 0, CalorieGoal: null)]);
        Assert.Contains("No food or water entries", BotStatisticsFormatter.Format(summary, russian: false), StringComparison.Ordinal);
        Assert.Throws<InvalidDataException>(() => BotStatisticsFormatter.Format(summary with { Days = [] }, russian: false));
    }
}
