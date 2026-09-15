using FoodDiary.Modules.Gamification.Domain.Contracts.Enums;
using FoodDiary.Modules.Gamification.Domain.Entities.Achievements;

namespace FoodDiary.Modules.Gamification.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class AchievementDefinitionAtomicityTests {
    [Fact]
    public void EntityUpdates_WhenLateValidationFails_AreAtomic() {
        var definition = AchievementDefinition.Create(
            "first_meal", "nutrition", AchievementMetric.TotalMeals, 1,
            "Первая еда", "First meal", "Описание", "Description", "meal", 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => definition.Update(
            category: "changed",
            metric: AchievementMetric.TotalMeals,
            threshold: 2,
            titleRu: new string('x', 161),
            titleEn: "Changed",
            descriptionRu: "Changed",
            descriptionEn: "Changed",
            icon: "icon",
            sortOrder: 1,
            isActive: true));
        Assert.Multiple(
            () => Assert.Equal("nutrition", definition.Category),
            () => Assert.Equal(1, definition.Version));
    }
}
