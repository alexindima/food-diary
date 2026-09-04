using FoodDiary.Application.Abstractions.DailyAdvices.Common;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class DailyAdviceErrorContractTests {
    [Fact]
    public void Factory_IsOwnedByModule() {
        Assert.Equal("FoodDiary.Modules.DailyAdvices.Application.Abstractions", typeof(DailyAdviceErrors).Assembly.GetName().Name);
        Assert.Equal("FoodDiary.Application.Abstractions.DailyAdvices.Common", typeof(DailyAdviceErrors).Namespace);
    }

    [Theory]
    [InlineData(null, "Daily advice items are not configured.")]
    [InlineData("", "Daily advice items are not configured for locale ''.")]
    [InlineData("ru", "Daily advice items are not configured for locale 'ru'.")]
    public void NotFound_PreservesLocaleAndClassification(string? locale, string message) {
        Error error = DailyAdviceErrors.NotFound(locale);
        Assert.Multiple(
            () => Assert.Equal("DailyAdvice.NotFound", error.Code),
            () => Assert.Equal(message, error.Message),
            () => Assert.Equal(ErrorKind.NotFound, error.Kind),
            () => Assert.Null(error.Details),
            () => Assert.Equal(DailyAdviceErrors.NotFound(locale: null), DailyAdviceErrors.NotFound()));
    }
}
