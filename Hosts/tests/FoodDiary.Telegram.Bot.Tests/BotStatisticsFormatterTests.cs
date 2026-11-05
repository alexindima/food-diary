using FoodDiary.Telegram.Bot.Operations;

namespace FoodDiary.Telegram.Bot.Tests;

[ExcludeFromCodeCoverage]
public sealed class BotStatisticsFormatterTests {
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Today_DisplaysLocalizedGoalsAndTimeZoneBesideTheReturnedDate(bool russian) {
        var summary = new BotDiaryStatistics("Asia/Tbilisi", 1, 1, 1200, 60.5, 40, 150, 12, 500, 2, 1200, 2000,
            [new BotDiaryStatisticsDay(new DateOnly(2026, 9, 12), 1200, 60.5, 40, 150, 12, 500, 2, 2100)]);
        string text = BotStatisticsFormatter.Format(summary, russian).Replace('\u00a0', ' ').Replace('\u202f', ' ');
        Assert.Multiple(
            () => Assert.StartsWith(russian ? "📊 <b>Сегодня · 12 сентября</b> (Asia/Tbilisi)\n" : "📊 <b>Today · September 12</b> (Asia/Tbilisi)\n", text, StringComparison.Ordinal),
            () => Assert.Contains(russian ? "🔥 <b>1 200 / 2 100 ккал</b>" : "🔥 <b>1,200 / 2,100 kcal</b>", text, StringComparison.Ordinal),
            () => Assert.Contains(russian ? "57% · Осталось 900 ккал" : "57% · Remaining 900 kcal", text, StringComparison.Ordinal),
            () => Assert.Contains(russian ? "🥩 Белки: 60,5 г" : "🥩 Protein: 60.5 g", text, StringComparison.Ordinal),
            () => Assert.Contains(russian ? "💧 Вода: 500 / 2 000 мл" : "💧 Water: 500 / 2,000 ml", text, StringComparison.Ordinal),
            () => Assert.DoesNotContain(russian ? "Часовой пояс:" : "Time zone:", text, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(null, 600, "🔥 <b>600 kcal</b>")]
    [InlineData(0, 600, "🔥 <b>600 kcal</b>")]
    [InlineData(500, 600, "120% · Over goal 100 kcal")]
    [InlineData(500, 500, "100% · Remaining 0 kcal")]
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Week_DisplaysLocalizedTotalsAndServerAverage(bool russian) {
        BotDiaryStatisticsDay[] days = [.. Enumerable.Range(0, 7).Select(offset => new BotDiaryStatisticsDay(
            new DateOnly(2026, 10, 30).AddDays(offset), 0, 0, 0, 0, 0, 0, 0, CalorieGoal: 2500))];
        var summary = new BotDiaryStatistics("UTC", 7, 2, 943, 58.1, 17.8, 137.8, 0, 400, 5, 134.7, DailyWaterGoalMl: 2000, days);
        string text = BotStatisticsFormatter.Format(summary, russian);
        string expected = russian
            ? """
              📊 <b>7 дней · 30 октября – 5 ноября</b> (UTC)
              🔥 <b>943 ккал</b>
              В среднем 134,7 ккал/день
              Дней с приёмами пищи: 2 из 7

              <b>Нутриенты</b>
              🥩 Белки: 58,1 г
              🥑 Жиры: 17,8 г
              🍞 Углеводы: 137,8 г

              💧 Вода: 400 мл
              🍽️ Приёмов пищи: 5
              """
            : """
              📊 <b>7 days · October 30 – November 5</b> (UTC)
              🔥 <b>943 kcal</b>
              Average 134.7 kcal/day
              Days with meals: 2 of 7

              <b>Nutrients</b>
              🥩 Protein: 58.1 g
              🥑 Fat: 17.8 g
              🍞 Carbs: 137.8 g

              💧 Water: 400 ml
              🍽️ Meals: 5
              """;
        Assert.Equal(expected, text, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void EmptyDay_IsExplicitAndInvalidResponseIsNotPresentedAsEmpty() {
        var summary = new BotDiaryStatistics("UTC", 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, DailyWaterGoalMl: null,
            [new BotDiaryStatisticsDay(new DateOnly(2026, 9, 12), 0, 0, 0, 0, 0, 0, 0, CalorieGoal: null)]);
        string text = BotStatisticsFormatter.Format(summary with { TimeZoneId = "UTC<&>" }, russian: false);
        Assert.Contains("(UTC&lt;&amp;&gt;)", text, StringComparison.Ordinal);
        Assert.Contains("No food or water entries", text, StringComparison.Ordinal);
        Assert.Throws<InvalidDataException>(() => BotStatisticsFormatter.Format(summary with { Days = [] }, russian: false));
    }
}
