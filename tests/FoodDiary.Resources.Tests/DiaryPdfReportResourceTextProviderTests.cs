using FoodDiary.Application.Abstractions.Export.Common;
using FoodDiary.Resources.Reports;

namespace FoodDiary.Resources.Tests;

[ExcludeFromCodeCoverage]
public sealed class DiaryPdfReportResourceTextProviderTests {
    [Theory]
    [InlineData(null, "en")]
    [InlineData("", "en")]
    [InlineData(" en-US ", "en")]
    [InlineData("ru", "ru")]
    [InlineData("ru-RU", "ru")]
    public void GetTexts_ResolvesSupportedCulture(string? locale, string expectedCulture) {
        var provider = new DiaryPdfReportResourceTextProvider();

        DiaryPdfReportTexts texts = provider.GetTexts(locale);

        Assert.Equal(expectedCulture, texts.CultureName);
        Assert.False(string.IsNullOrWhiteSpace(texts.ReportTitle));
        Assert.False(string.IsNullOrWhiteSpace(texts.PeriodLabel));
        Assert.False(string.IsNullOrWhiteSpace(texts.MealsTitle));
        Assert.False(string.IsNullOrWhiteSpace(texts.GeneratedByPrefix));
    }

    [Fact]
    public void GetTexts_WhenUnsupportedLocale_FallsBackToEnglish() {
        var provider = new DiaryPdfReportResourceTextProvider();

        DiaryPdfReportTexts texts = provider.GetTexts("ka-GE");

        Assert.Equal("en", texts.CultureName);
        Assert.False(string.IsNullOrWhiteSpace(texts.NoMealsMessage));
    }
}
