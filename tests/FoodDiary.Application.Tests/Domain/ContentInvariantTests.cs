using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Entities.FavoriteMeals;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Domain;

public class ContentInvariantTests {
    [Fact]
    public void NutritionLesson_Create_WithBlankTitle_Throws() {
        Assert.Throws<ArgumentException>(() =>
            NutritionLesson.Create("   ", "Content", null, "en",
                LessonCategory.Macronutrients, LessonDifficulty.Beginner, 5));
    }

    [Fact]
    public void NutritionLesson_Create_WithBlankContent_Throws() {
        Assert.Throws<ArgumentException>(() =>
            NutritionLesson.Create("Title", "   ", null, "en",
                LessonCategory.Macronutrients, LessonDifficulty.Beginner, 5));
    }

    [Fact]
    public void NutritionLesson_Create_WithBlankLocale_Throws() {
        Assert.Throws<ArgumentException>(() =>
            NutritionLesson.Create("Title", "Content", null, "   ",
                LessonCategory.Macronutrients, LessonDifficulty.Beginner, 5));
    }

    [Fact]
    public void NutritionLesson_Create_WithTooLongTitle_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NutritionLesson.Create(new string('t', 257), "Content", null, "en",
                LessonCategory.Macronutrients, LessonDifficulty.Beginner, 5));
    }

    [Fact]
    public void NutritionLesson_Create_WithTooLongContent_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NutritionLesson.Create("Title", new string('c', 8193), null, "en",
                LessonCategory.Macronutrients, LessonDifficulty.Beginner, 5));
    }

    [Fact]
    public void NutritionLesson_Create_NormalizesValues() {
        var lesson = NutritionLesson.Create(
            "  Vitamins  ", "  Body needs vitamins  ", "  Short summary  ", "  EN  ",
            LessonCategory.Micronutrients, LessonDifficulty.Intermediate, 10, 3);

        Assert.Equal("Vitamins", lesson.Title);
        Assert.Equal("Body needs vitamins", lesson.Content);
        Assert.Equal("Short summary", lesson.Summary);
        Assert.Equal("en", lesson.Locale);
        Assert.Equal(LessonCategory.Micronutrients, lesson.Category);
        Assert.Equal(LessonDifficulty.Intermediate, lesson.Difficulty);
        Assert.Equal(10, lesson.EstimatedReadMinutes);
        Assert.Equal(3, lesson.SortOrder);
    }

    [Fact]
    public void NutritionLesson_Create_WithZeroEstimatedReadMinutes_ClampsTo1() {
        var lesson = NutritionLesson.Create(
            "Title", "Content", null, "en",
            LessonCategory.Macronutrients, LessonDifficulty.Beginner, 0);

        Assert.Equal(1, lesson.EstimatedReadMinutes);
    }

    [Fact]
    public void NutritionLesson_Create_WithNegativeSortOrder_ClampsTo0() {
        var lesson = NutritionLesson.Create(
            "Title", "Content", null, "en",
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
        var longSummary = new string('s', 600);
        var lesson = NutritionLesson.Create(
            "Title", "Content", longSummary, "en",
            LessonCategory.Macronutrients, LessonDifficulty.Beginner, 5);

        Assert.Equal(512, lesson.Summary!.Length);
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

        fav.UpdateName(null);

        Assert.Null(fav.Name);
        Assert.NotNull(fav.ModifiedOnUtc);
    }
}
