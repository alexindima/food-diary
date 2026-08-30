using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Entities.FavoriteMeals;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public class ContentInvariantTests {
    [Fact]
    public void DailyAdvice_Create_NormalizesValues() {
        var advice = DailyAdvice.Create("  Drink water  ", "  ru-RU  ", weight: 1, tag: "  hydration  ");

        Assert.Multiple(
            () => Assert.NotEqual(DailyAdviceId.Empty, advice.Id),
            () => Assert.Equal("Drink water", advice.Value),
            () => Assert.Equal("ru", advice.Locale),
            () => Assert.Equal(1, advice.Weight),
            () => Assert.Equal("hydration", advice.Tag),
            () => Assert.NotEqual(default, advice.CreatedOnUtc));
    }

    [Fact]
    public void DailyAdvice_Create_WithInvalidValues_Throws() {
        Assert.Throws<ArgumentException>(() => DailyAdvice.Create(" ", "en"));
        Assert.Throws<ArgumentException>(() => DailyAdvice.Create("Advice", " "));
        Assert.Throws<ArgumentOutOfRangeException>(() => DailyAdvice.Create(new string('v', 513), "en"));
        Assert.Throws<ArgumentOutOfRangeException>(() => DailyAdvice.Create("Advice", "de"));
        Assert.Throws<ArgumentOutOfRangeException>(() => DailyAdvice.Create("Advice", "en", weight: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => DailyAdvice.Create("Advice", "en", tag: new string('t', 65)));
    }

    [Fact]
    public void DailyAdvice_Update_WithSameNormalizedValues_DoesNotSetModifiedOnUtc() {
        var advice = DailyAdvice.Create("Advice", "en", weight: 2, tag: "tag");

        advice.Update(value: "  Advice  ", locale: "en-US", weight: 2, tag: "  tag  ");

        Assert.Null(advice.ModifiedOnUtc);
    }

    [Fact]
    public void DailyAdvice_Update_WithDifferentValues_NormalizesAndSetsModifiedOnUtc() {
        var advice = DailyAdvice.Create("Advice", "en", weight: 2, tag: "tag");

        advice.Update(value: "  New advice  ", locale: "ru", weight: 3, tag: "  new-tag  ");

        Assert.Multiple(
            () => Assert.Equal("New advice", advice.Value),
            () => Assert.Equal("ru", advice.Locale),
            () => Assert.Equal(3, advice.Weight),
            () => Assert.Equal("new-tag", advice.Tag));
        Assert.NotNull(advice.ModifiedOnUtc);
    }

    [Fact]
    public void DailyAdvice_Update_WithClearTag_ClearsTagAndRejectsConflicts() {
        var advice = DailyAdvice.Create("Advice", "en", tag: "tag");

        Assert.Throws<ArgumentException>(() => advice.Update(tag: "tag", clearTag: true));

        advice.Update(clearTag: true);

        Assert.Null(advice.Tag);
        Assert.NotNull(advice.ModifiedOnUtc);
    }

    [Fact]
    public void DailyAdvice_Update_WithInvalidValues_Throws() {
        var advice = DailyAdvice.Create("Advice", "en");

        Assert.Throws<ArgumentException>(() => advice.Update(value: " "));
        Assert.Throws<ArgumentException>(() => advice.Update(locale: " "));
        Assert.Throws<ArgumentOutOfRangeException>(() => advice.Update(value: new string('v', 513)));
        Assert.Throws<ArgumentOutOfRangeException>(() => advice.Update(locale: "de"));
        Assert.Throws<ArgumentOutOfRangeException>(() => advice.Update(weight: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => advice.Update(tag: new string('t', 65)));
    }

    [Fact]
    public void FavoriteMeal_Create_WithEmptyUserId_Throws() {
        Assert.Throws<ArgumentException>(() =>
            FavoriteMeal.Create(UserId.Empty, MealId.New()));
    }

    [Fact]
    public void FavoriteMeal_Create_WithEmptyMealId_Throws() {
        Assert.Throws<ArgumentException>(() =>
            FavoriteMeal.Create(UserId.New(), MealId.Empty));
    }

    [Fact]
    public void FavoriteMeal_Create_WithName_TrimsName() {
        var fav = FavoriteMeal.Create(UserId.New(), MealId.New(), "  My Breakfast  ");

        Assert.Equal("My Breakfast", fav.Name);
    }

    [Fact]
    public void FavoriteMeal_Create_WithWhitespaceName_SetsNull() {
        var fav = FavoriteMeal.Create(UserId.New(), MealId.New(), "   ");

        Assert.Null(fav.Name);
    }

    [Fact]
    public void FavoriteMeal_UpdateName_WithNewValue_SetsModifiedOnUtc() {
        var fav = FavoriteMeal.Create(UserId.New(), MealId.New(), "Old");

        fav.UpdateName("New");

        Assert.Equal("New", fav.Name);
        Assert.NotNull(fav.ModifiedOnUtc);
    }

    [Fact]
    public void FavoriteMeal_UpdateName_WithSameValue_DoesNotSetModifiedOnUtc() {
        var fav = FavoriteMeal.Create(UserId.New(), MealId.New(), "Same");

        fav.UpdateName("Same");

        Assert.Null(fav.ModifiedOnUtc);
    }

    [Fact]
    public void FavoriteMeal_UpdateName_WithNull_ClearsName() {
        var fav = FavoriteMeal.Create(UserId.New(), MealId.New(), "Name");

        fav.UpdateName(name: null);

        Assert.Null(fav.Name);
        Assert.NotNull(fav.ModifiedOnUtc);
    }
}
