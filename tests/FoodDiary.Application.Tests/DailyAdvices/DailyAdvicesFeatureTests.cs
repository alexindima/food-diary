using System.Reflection;
using FoodDiary.Application.DailyAdvices.Queries.GetDailyAdvice;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.DailyAdvices;

public class DailyAdvicesFeatureTests {
    [Fact]
    public async Task GetDailyAdviceQueryValidator_WithEmptyUserId_Fails() {
        var validator = new GetDailyAdviceQueryValidator();
        var query = new GetDailyAdviceQuery(UserId.Empty, DateTime.UtcNow, "en");

        var result = await validator.ValidateAsync(query);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task GetDailyAdviceQueryValidator_WithValidInput_Passes() {
        var validator = new GetDailyAdviceQueryValidator();
        var query = new GetDailyAdviceQuery(UserId.New(), DateTime.UtcNow, "ru-RU");

        var result = await validator.ValidateAsync(query);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void DailyAdviceSelector_NormalizeLocale_UnsupportedLocaleFallsBackToEn() {
        var normalized = InvokeNormalizeLocale("de-DE");

        Assert.Equal("en", normalized);
    }

    [Fact]
    public void DailyAdviceSelector_SelectForDate_ReturnsAdviceFromRequestedLocale() {
        var advices = new List<DailyAdvice> {
            DailyAdvice.Create("Hydrate", "en", weight: 1),
            DailyAdvice.Create("Walk", "en", weight: 1),
            DailyAdvice.Create("Пей воду", "ru", weight: 1)
        };

        var date = new DateTime(2026, 2, 20, 0, 0, 0, DateTimeKind.Utc);
        var selected = InvokeSelectForDate(advices, date, "ru-RU");

        Assert.NotNull(selected);
        Assert.Equal("ru", selected!.Locale);
    }

    [Fact]
    public void DailyAdviceSelector_SelectForDate_WhenLocaleHasNoAdvice_ReturnsNull() {
        var advices = new List<DailyAdvice> {
            DailyAdvice.Create("Hydrate", "en", weight: 1)
        };

        var selected = InvokeSelectForDate(advices, DateTime.UtcNow, "ru");

        Assert.Null(selected);
    }

    private static string InvokeNormalizeLocale(string locale) {
        var selectorType = GetSelectorType();
        var method = selectorType.GetMethod("NormalizeLocale", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.NotNull(method);

        return (string)method!.Invoke(null, [locale])!;
    }

    private static DailyAdvice? InvokeSelectForDate(IReadOnlyList<DailyAdvice> advices, DateTime date, string locale) {
        var selectorType = GetSelectorType();
        var method = selectorType.GetMethod("SelectForDate", BindingFlags.Static | BindingFlags.Public);
        Assert.NotNull(method);

        return (DailyAdvice?)method!.Invoke(null, [advices, date, locale]);
    }

    private static Type GetSelectorType() {
        var selectorType = Type.GetType("FoodDiary.Application.DailyAdvices.Services.DailyAdviceSelector, FoodDiary.Application");
        Assert.NotNull(selectorType);
        return selectorType!;
    }
}
