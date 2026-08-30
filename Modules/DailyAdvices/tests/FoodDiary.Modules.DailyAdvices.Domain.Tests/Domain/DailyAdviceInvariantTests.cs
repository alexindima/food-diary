using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.DailyAdvices.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class DailyAdviceInvariantTests {
    [Fact]
    public void Create_NormalizesValues() {
        var advice = DailyAdvice.Create("  Drink water  ", "  ru-RU  ", tag: "  hydration  ");
        Assert.Multiple(
            () => Assert.NotEqual(DailyAdviceId.Empty, advice.Id),
            () => Assert.Equal("Drink water", advice.Value),
            () => Assert.Equal("ru", advice.Locale),
            () => Assert.Equal("hydration", advice.Tag));
    }

    [Fact]
    public void Create_WithInvalidValues_Throws() {
        Assert.Throws<ArgumentException>(() => DailyAdvice.Create(" ", "en"));
        Assert.Throws<ArgumentException>(() => DailyAdvice.Create("Advice", " "));
        Assert.Throws<ArgumentOutOfRangeException>(() => DailyAdvice.Create(new string('a', 513), "en"));
        Assert.Throws<ArgumentOutOfRangeException>(() => DailyAdvice.Create("Advice", "en", tag: new string('a', 65)));
        Assert.Throws<ArgumentOutOfRangeException>(() => DailyAdvice.Create("Advice", "de"));
        Assert.Throws<ArgumentOutOfRangeException>(() => DailyAdvice.Create("Advice", "en", weight: 0));
    }

    [Fact]
    public void Update_NormalizesAndTracksChanges() {
        var advice = DailyAdvice.Create("Advice", "en", weight: 2, tag: "tag");
        advice.Update(value: " New advice ", locale: "ru", weight: 3, tag: "new-tag");
        Assert.Multiple(
            () => Assert.Equal("New advice", advice.Value),
            () => Assert.Equal("ru", advice.Locale),
            () => Assert.Equal(3, advice.Weight),
            () => Assert.Equal("new-tag", advice.Tag),
            () => Assert.NotNull(advice.ModifiedOnUtc));
    }

    [Fact]
    public void Update_ClearTagRejectsConflictingValue() {
        var advice = DailyAdvice.Create("Advice", "en", tag: "tag");
        Assert.Throws<ArgumentException>(() => advice.Update(tag: "tag", clearTag: true));
        advice.Update(clearTag: true);
        Assert.Null(advice.Tag);
    }

    [Fact]
    public void Update_WithEquivalentNormalizedValues_DoesNotTrackChanges() {
        var advice = DailyAdvice.Create("Advice", "en", weight: 2, tag: "tag");
        advice.Update(value: " Advice ", locale: "en-US", weight: 2, tag: " tag ");
        Assert.Null(advice.ModifiedOnUtc);
    }

    [Fact]
    public void Update_WithInvalidValues_Throws() {
        var advice = DailyAdvice.Create("Advice", "en");
        Assert.Throws<ArgumentException>(() => advice.Update(value: " "));
        Assert.Throws<ArgumentException>(() => advice.Update(locale: " "));
        Assert.Throws<ArgumentOutOfRangeException>(() => advice.Update(value: new string('a', 513)));
        Assert.Throws<ArgumentOutOfRangeException>(() => advice.Update(locale: "de"));
        Assert.Throws<ArgumentOutOfRangeException>(() => advice.Update(weight: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => advice.Update(tag: new string('a', 65)));
    }
}
