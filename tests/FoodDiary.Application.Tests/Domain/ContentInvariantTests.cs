using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Entities.FavoriteMeals;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Domain;

[ExcludeFromCodeCoverage]
public class ContentInvariantTests {
    [Fact]
    public void NutritionLesson_Create_WithBlankTitle_Throws() {
        Assert.Throws<ArgumentException>(() =>
            NutritionLesson.Create("   ", "Content", summary: null, "en",
                LessonCategory.Macronutrients, LessonDifficulty.Beginner, 5));
    }

    [Fact]
    public void NutritionLesson_Create_WithBlankContent_Throws() {
        Assert.Throws<ArgumentException>(() =>
            NutritionLesson.Create("Title", "   ", summary: null, "en",
                LessonCategory.Macronutrients, LessonDifficulty.Beginner, 5));
    }

    [Fact]
    public void NutritionLesson_Create_WithBlankLocale_Throws() {
        Assert.Throws<ArgumentException>(() =>
            NutritionLesson.Create("Title", "Content", summary: null, "   ",
                LessonCategory.Macronutrients, LessonDifficulty.Beginner, 5));
    }

    [Fact]
    public void NutritionLesson_Create_WithTooLongTitle_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NutritionLesson.Create(new string('t', 257), "Content", summary: null, "en",
                LessonCategory.Macronutrients, LessonDifficulty.Beginner, 5));
    }

    [Fact]
    public void NutritionLesson_Create_WithTooLongContent_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NutritionLesson.Create("Title", new string('c', 65537), summary: null, "en",
                LessonCategory.Macronutrients, LessonDifficulty.Beginner, 5));
    }

    [Fact]
    public void NutritionLesson_Create_NormalizesValues() {
        var lesson = NutritionLesson.Create(
            "  Vitamins  ", "  Body needs vitamins  ", "  Short summary  ", "  EN  ",
            LessonCategory.Micronutrients, LessonDifficulty.Intermediate, 10, 3);

        Assert.Multiple(
            () => Assert.Equal("Vitamins", lesson.Title),
            () => Assert.Equal("Body needs vitamins", lesson.Content),
            () => Assert.Equal("Short summary", lesson.Summary),
            () => Assert.Equal("en", lesson.Locale),
            () => Assert.Equal(LessonCategory.Micronutrients, lesson.Category),
            () => Assert.Equal(LessonDifficulty.Intermediate, lesson.Difficulty),
            () => Assert.Equal(10, lesson.EstimatedReadMinutes),
            () => Assert.Equal(3, lesson.SortOrder));
    }

    [Fact]
    public void NutritionLesson_Create_WithZeroEstimatedReadMinutes_ClampsTo1() {
        var lesson = NutritionLesson.Create(
            "Title", "Content", summary: null, "en",
            LessonCategory.Macronutrients, LessonDifficulty.Beginner, 0);

        Assert.Equal(1, lesson.EstimatedReadMinutes);
    }

    [Fact]
    public void NutritionLesson_Create_WithNegativeSortOrder_ClampsTo0() {
        var lesson = NutritionLesson.Create(
            "Title", "Content", summary: null, "en",
            LessonCategory.Macronutrients, LessonDifficulty.Beginner, 5, -1);

        Assert.Equal(0, lesson.SortOrder);
    }

    [Fact]
    public void NutritionLesson_Create_WithWhitespaceSummary_SetsNull() {
        var lesson = NutritionLesson.Create(
            "Title", "Content", "   ", "en",
            LessonCategory.Macronutrients, LessonDifficulty.Beginner, 5);

        Assert.Null(lesson.Summary);
    }

    [Fact]
    public void NutritionLesson_Create_TruncatesLongSummary() {
        string longSummary = new('s', 600);
        var lesson = NutritionLesson.Create(
            "Title", "Content", longSummary, "en",
            LessonCategory.Macronutrients, LessonDifficulty.Beginner, 5);

        Assert.Equal(512, lesson.Summary!.Length);
    }

    [Fact]
    public void NutritionLesson_Update_WithSameNormalizedValues_DoesNotSetModifiedOnUtc() {
        var lesson = NutritionLesson.Create(
            "Title",
            "Content",
            "Summary",
            "en",
            LessonCategory.Macronutrients,
            LessonDifficulty.Beginner,
            5,
            1);

        lesson.Update(
            "  Title  ",
            "  Content  ",
            "  Summary  ",
            "EN",
            LessonCategory.Macronutrients,
            LessonDifficulty.Beginner,
            5,
            1);

        Assert.Null(lesson.ModifiedOnUtc);
    }

    [Fact]
    public void NutritionLesson_Update_WithDifferentValues_NormalizesAndSetsModifiedOnUtc() {
        var lesson = NutritionLesson.Create(
            "Title",
            "Content",
            summary: null,
            "en",
            LessonCategory.Macronutrients,
            LessonDifficulty.Beginner,
            5,
            1);

        lesson.Update(
            "  New title  ",
            "  New content  ",
            "  New summary  ",
            "RU",
            LessonCategory.Micronutrients,
            LessonDifficulty.Advanced,
            estimatedReadMinutes: 0,
            sortOrder: -1);

        Assert.Multiple(
            () => Assert.Equal("New title", lesson.Title),
            () => Assert.Equal("New content", lesson.Content),
            () => Assert.Equal("New summary", lesson.Summary),
            () => Assert.Equal("ru", lesson.Locale),
            () => Assert.Equal(LessonCategory.Micronutrients, lesson.Category),
            () => Assert.Equal(LessonDifficulty.Advanced, lesson.Difficulty),
            () => Assert.Equal(1, lesson.EstimatedReadMinutes),
            () => Assert.Equal(0, lesson.SortOrder));
        Assert.NotNull(lesson.ModifiedOnUtc);
    }

    [Fact]
    public void NutritionLesson_Update_WithInvalidValues_Throws() {
        var lesson = NutritionLesson.Create(
            "Title",
            "Content",
            summary: null,
            "en",
            LessonCategory.Macronutrients,
            LessonDifficulty.Beginner,
            5);

        Assert.Throws<ArgumentException>(() => lesson.Update(" ", "Content", summary: null, "en", LessonCategory.Macronutrients, LessonDifficulty.Beginner, 5, 0));
        Assert.Throws<ArgumentException>(() => lesson.Update("Title", " ", summary: null, "en", LessonCategory.Macronutrients, LessonDifficulty.Beginner, 5, 0));
        Assert.Throws<ArgumentException>(() => lesson.Update("Title", "Content", summary: null, " ", LessonCategory.Macronutrients, LessonDifficulty.Beginner, 5, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => lesson.Update(new string('t', 257), "Content", summary: null, "en", LessonCategory.Macronutrients, LessonDifficulty.Beginner, 5, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => lesson.Update("Title", new string('c', 65537), summary: null, "en", LessonCategory.Macronutrients, LessonDifficulty.Beginner, 5, 0));
    }

    [Fact]
    public void DailyAdvice_Create_NormalizesValuesAndClampsWeight() {
        var advice = DailyAdvice.Create("  Drink water  ", "  ru-RU  ", weight: 0, tag: "  hydration  ");

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

        advice.Update(value: "  New advice  ", locale: "ru", weight: 0, tag: "  new-tag  ");

        Assert.Multiple(
            () => Assert.Equal("New advice", advice.Value),
            () => Assert.Equal("ru", advice.Locale),
            () => Assert.Equal(1, advice.Weight),
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
        Assert.Throws<ArgumentOutOfRangeException>(() => advice.Update(tag: new string('t', 65)));
    }

    [Fact]
    public void UserLessonProgress_Create_WithEmptyUserId_Throws() {
        Assert.Throws<ArgumentException>(() =>
            UserLessonProgress.Create(UserId.Empty, NutritionLessonId.New(), DateTime.UtcNow));
    }

    [Fact]
    public void UserLessonProgress_Create_WithLocalTimestamp_NormalizesToUtc() {
        var localTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);
        var progress = UserLessonProgress.Create(UserId.New(), NutritionLessonId.New(), localTime);

        Assert.Equal(DateTimeKind.Utc, progress.ReadAtUtc.Kind);
    }

    [Fact]
    public void UserLessonProgress_Create_WithUnspecifiedKind_SpecifiesAsUtc() {
        var unspecified = new DateTime(2026, 3, 15, 12, 0, 0, DateTimeKind.Unspecified);
        var progress = UserLessonProgress.Create(UserId.New(), NutritionLessonId.New(), unspecified);

        Assert.Equal(DateTimeKind.Utc, progress.ReadAtUtc.Kind);
        Assert.Equal(unspecified, progress.ReadAtUtc, TimeSpan.FromSeconds(1));
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
