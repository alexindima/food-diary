using FoodDiary.Domain.Entities.Achievements;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Domain.Tests.Achievements;

[ExcludeFromCodeCoverage]
public sealed class AchievementDefinitionTests {
    [Fact]
    public void Create_NormalizesCodesAndStartsAtVersionOne() {
        var definition = AchievementDefinition.Create(
            " CUSTOM_10 ", " Habits ", AchievementMetric.TotalMeals, 10,
            "Название", "Title", "Описание", "Description", " Trophy ", 5);

        Assert.Multiple(
            () => Assert.Equal("custom_10", definition.Key),
            () => Assert.Equal("habits", definition.Category),
            () => Assert.Equal("trophy", definition.Icon),
            () => Assert.Equal(1, definition.Version),
            () => Assert.True(definition.IsActive));
    }

    [Fact]
    public void Update_IncrementsVersionAndCanDisableDefinition() {
        var definition = AchievementDefinition.Create(
            "custom_10", "habits", AchievementMetric.TotalMeals, 10,
            "Название", "Title", "Описание", "Description", "trophy", 5);

        definition.Update(
            "nutrition", AchievementMetric.TotalMeals, 20,
            "Новое название", "New title", "Новое описание", "New description", "restaurant", 6, isActive: false);

        Assert.Multiple(
            () => Assert.Equal(2, definition.Version),
            () => Assert.Equal(20, definition.Threshold),
            () => Assert.False(definition.IsActive));
    }

    [Theory]
    [InlineData("bad key")]
    [InlineData("bad.key")]
    public void Create_WithUnsafeKey_Throws(string key) {
        Assert.Throws<ArgumentException>(() => AchievementDefinition.Create(
            key, "habits", AchievementMetric.TotalMeals, 10,
            "Название", "Title", "Описание", "Description", "trophy", 0));
    }

    [Fact]
    public void Create_WithInvalidMetric_Throws() => Assert.Throws<ArgumentOutOfRangeException>(() => Create(metric: (AchievementMetric)int.MaxValue));

    [Fact]
    public void Create_WithNonPositiveThreshold_Throws() => Assert.Throws<ArgumentOutOfRangeException>(() => Create(threshold: 0));

    [Fact]
    public void Create_WithNegativeSortOrder_Throws() => Assert.Throws<ArgumentOutOfRangeException>(() => Create(sortOrder: -1));

    [Fact]
    public void Create_WithMissingRequiredValue_Throws() => Assert.Throws<ArgumentException>(() => Create(titleEn: " "));

    private static AchievementDefinition Create(
        AchievementMetric metric = AchievementMetric.TotalMeals,
        int threshold = 10,
        int sortOrder = 0,
        string titleEn = "Title") => AchievementDefinition.Create(
            "custom_10", "habits", metric, threshold,
            "Title RU", titleEn, "Description RU", "Description", "trophy", sortOrder);
}
