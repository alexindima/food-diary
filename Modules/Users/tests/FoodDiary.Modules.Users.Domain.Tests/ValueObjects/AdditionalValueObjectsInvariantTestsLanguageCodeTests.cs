using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Tests.ValueObjects;

[ExcludeFromCodeCoverage]
public sealed class AdditionalValueObjectsInvariantTestsLanguageCodeTests {
    [Theory]
    [InlineData(" en ", "en")]
    [InlineData("RU", "ru")]
    public void LanguageCode_TryParse_WithSupportedValues_Normalizes(string value, string expected) {
        bool parsed = LanguageCode.TryParse(value, out LanguageCode language);

        Assert.Multiple(
            () => Assert.True(parsed),
            () => Assert.Equal(expected, language.Value),
            () => Assert.Equal(expected, language.ToString()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("de")]
    public void LanguageCode_TryParse_WithUnsupportedValues_ReturnsFalse(string? value) {
        bool parsed = LanguageCode.TryParse(value, out LanguageCode language);

        Assert.False(parsed);
        Assert.Equal(default, language);
    }

    [Theory]
    [InlineData(null, "en")]
    [InlineData(" ", "en")]
    [InlineData("ru-RU", "ru")]
    [InlineData("en-US", "en")]
    public void LanguageCode_FromPreferred_ReturnsSupportedLanguage(string? value, string expected) {
        var language = LanguageCode.FromPreferred(value);

        Assert.Equal(expected, language.Value);
    }
}
