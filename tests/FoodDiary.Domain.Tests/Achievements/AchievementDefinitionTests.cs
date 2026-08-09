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
}
