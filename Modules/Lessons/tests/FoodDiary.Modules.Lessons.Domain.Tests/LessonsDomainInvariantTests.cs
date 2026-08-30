using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class LessonsDomainInvariantTests {
    private static readonly DateTime Now = new(2026, 8, 19, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void NutritionLesson_Create_WithBlankTitle_Throws() {
        Assert.Throws<ArgumentException>(() => NutritionLesson.Create("   ", "Content", summary: null, "en", LessonCategory.Macronutrients, LessonDifficulty.Beginner, 5));
    }

    [Fact]
    public void NutritionLesson_Create_WithBlankContent_Throws() {
        Assert.Throws<ArgumentException>(() => NutritionLesson.Create("Title", "   ", summary: null, "en", LessonCategory.Macronutrients, LessonDifficulty.Beginner, 5));
    }

    [Fact]
    public void NutritionLesson_Create_WithBlankLocale_Throws() {
        Assert.Throws<ArgumentException>(() => NutritionLesson.Create("Title", "Content", summary: null, "   ", LessonCategory.Macronutrients, LessonDifficulty.Beginner, 5));
    }

    [Fact]
    public void NutritionLesson_Create_WithTooLongTitle_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() => NutritionLesson.Create(new string('t', 257), "Content", summary: null, "en", LessonCategory.Macronutrients, LessonDifficulty.Beginner, 5));
    }

    [Fact]
    public void NutritionLesson_Create_WithTooLongContent_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() => NutritionLesson.Create("Title", new string('c', 65537), summary: null, "en", LessonCategory.Macronutrients, LessonDifficulty.Beginner, 5));
    }

    [Fact]
    public void NutritionLesson_Create_NormalizesValues() {
        var lesson = NutritionLesson.Create("  Vitamins  ", "  Body needs vitamins  ", "  Short summary  ", "  EN  ", LessonCategory.Micronutrients, LessonDifficulty.Intermediate, 10, 3);

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
    public void NutritionLesson_Create_WithZeroEstimatedReadMinutes_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() => NutritionLesson.Create("Title", "Content", summary: null, "en", LessonCategory.Macronutrients, LessonDifficulty.Beginner, 0));
    }

    [Fact]
    public void NutritionLesson_Create_WithNegativeSortOrder_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() => NutritionLesson.Create("Title", "Content", summary: null, "en", LessonCategory.Macronutrients, LessonDifficulty.Beginner, 5, sortOrder: -1));
    }

    [Fact]
    public void NutritionLesson_Create_WithWhitespaceSummary_SetsNull() {
        var lesson = NutritionLesson.Create("Title", "Content", "   ", "en", LessonCategory.Macronutrients, LessonDifficulty.Beginner, 5);
        Assert.Null(lesson.Summary);
    }

    [Fact]
    public void NutritionLesson_Create_WithLongSummary_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() => NutritionLesson.Create("Title", "Content", new string('s', 600), "en", LessonCategory.Macronutrients, LessonDifficulty.Beginner, 5));
    }

    [Fact]
    public void NutritionLesson_Update_WithSameNormalizedValues_DoesNotSetModifiedOnUtc() {
        var lesson = NutritionLesson.Create("Title", "Content", "Summary", "en", LessonCategory.Macronutrients, LessonDifficulty.Beginner, 5, 1);
        lesson.Update("  Title  ", "  Content  ", "  Summary  ", "EN", LessonCategory.Macronutrients, LessonDifficulty.Beginner, 5, 1);
        Assert.Null(lesson.ModifiedOnUtc);
    }

    [Fact]
    public void NutritionLesson_Update_WithDifferentValues_NormalizesAndSetsModifiedOnUtc() {
        var lesson = NutritionLesson.Create("Title", "Content", summary: null, "en", LessonCategory.Macronutrients, LessonDifficulty.Beginner, 5, sortOrder: 1);
        lesson.Update("  New title  ", "  New content  ", "  New summary  ", "RU", LessonCategory.Micronutrients, LessonDifficulty.Advanced, 6, 2);

        Assert.Multiple(
            () => Assert.Equal("New title", lesson.Title),
            () => Assert.Equal("New content", lesson.Content),
            () => Assert.Equal("New summary", lesson.Summary),
            () => Assert.Equal("ru", lesson.Locale),
            () => Assert.Equal(LessonCategory.Micronutrients, lesson.Category),
            () => Assert.Equal(LessonDifficulty.Advanced, lesson.Difficulty),
            () => Assert.Equal(6, lesson.EstimatedReadMinutes),
            () => Assert.Equal(2, lesson.SortOrder),
            () => Assert.NotNull(lesson.ModifiedOnUtc));
    }

    [Fact]
    public void NutritionLesson_Update_WithInvalidValues_Throws() {
        var lesson = NutritionLesson.Create("Title", "Content", summary: null, "en", LessonCategory.Macronutrients, LessonDifficulty.Beginner, 5);

        Assert.Throws<ArgumentException>(() => lesson.Update(" ", "Content", summary: null, "en", LessonCategory.Macronutrients, LessonDifficulty.Beginner, 5, sortOrder: 0));
        Assert.Throws<ArgumentException>(() => lesson.Update("Title", " ", summary: null, "en", LessonCategory.Macronutrients, LessonDifficulty.Beginner, 5, sortOrder: 0));
        Assert.Throws<ArgumentException>(() => lesson.Update("Title", "Content", summary: null, " ", LessonCategory.Macronutrients, LessonDifficulty.Beginner, 5, sortOrder: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => lesson.Update(new string('t', 257), "Content", summary: null, "en", LessonCategory.Macronutrients, LessonDifficulty.Beginner, 5, sortOrder: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => lesson.Update("Title", new string('c', 65537), summary: null, "en", LessonCategory.Macronutrients, LessonDifficulty.Beginner, 5, sortOrder: 0));
    }

    [Fact]
    public void UserLessonProgress_Create_WithEmptyUserId_Throws() {
        Assert.Throws<ArgumentException>(() => UserLessonProgress.Create(UserId.Empty, NutritionLessonId.New(), DateTime.UtcNow));
    }

    [Fact]
    public void UserLessonProgress_Create_WithEmptyLessonId_Throws() {
        Assert.Throws<ArgumentException>(() => UserLessonProgress.Create(UserId.New(), NutritionLessonId.Empty, Now));
    }

    [Fact]
    public void UserLessonProgress_Create_WithLocalTimestamp_NormalizesToUtc() {
        var localTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);
        var progress = UserLessonProgress.Create(UserId.New(), NutritionLessonId.New(), localTime);
        Assert.Equal(DateTimeKind.Utc, progress.ReadAtUtc.Kind);
    }

    [Fact]
    public void UserLessonProgress_Create_WithUnspecifiedKind_Throws() {
        DateTime unspecified = new(2026, 3, 15, 12, 0, 0, DateTimeKind.Unspecified);
        Assert.Throws<ArgumentOutOfRangeException>(() => UserLessonProgress.Create(UserId.New(), NutritionLessonId.New(), unspecified));
    }
}
