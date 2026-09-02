using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Tests.ValueObjects;

[ExcludeFromCodeCoverage]
public sealed class ValueObjectsInvariantTestsLanguageCodeTests {
    [Fact]
    public void LanguageCode_TryParse_AndFromPreferred_WorkAsExpected() {
        bool parsed = LanguageCode.TryParse("  EN  ", out LanguageCode en);
        var preferredRu = LanguageCode.FromPreferred("ru-RU");
        var preferredDefault = LanguageCode.FromPreferred("de-DE");

        Assert.Multiple(
            () => Assert.True(parsed),
            () => Assert.Equal("en", en.Value),
            () => Assert.Equal("ru", preferredRu.Value),
            () => Assert.Equal("en", preferredDefault.Value));
    }
}
