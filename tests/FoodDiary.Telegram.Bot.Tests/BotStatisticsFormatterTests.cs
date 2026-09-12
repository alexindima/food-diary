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
        string text = BotStatisticsFormatter.Format(summary, russian);
        Assert.Multiple(
            () => Assert.Contains("1200 / 2100", text, StringComparison.Ordinal),
            () => Assert.Contains("500 / 2000", text, StringComparison.Ordinal),
            () => Assert.Contains("Asia/Tbilisi", text, StringComparison.Ordinal),
            () => Assert.Contains(russian ? "Сегодня" : "Today", text, StringComparison.Ordinal));
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
