using FoodDiary.Modules.Gamification.Domain.Contracts.Enums;
using FoodDiary.Modules.Gamification.Domain.Entities.Achievements;
using System.Reflection;

namespace FoodDiary.Modules.Gamification.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class AchievementVersionOverflowTests {
    private static void SetProperty<T>(T instance, string propertyName, object value) {
        PropertyInfo property = typeof(T).GetProperty(propertyName) ?? throw new InvalidOperationException($"Property {propertyName} was not found.");
        property.SetValue(instance, value);
    }

    [Fact]
    public void VersionedEntities_RejectOverflowBeforeMutation() {
        var definition = AchievementDefinition.Create(
            "meals_10",
            "habits",
            AchievementMetric.TotalMeals,
            threshold: 10,
            "Название",
            "Title",
            "Описание",
            "Description",
            "trophy",
            sortOrder: 1);
        SetProperty(definition, nameof(AchievementDefinition.Version), int.MaxValue);
        Assert.Throws<InvalidOperationException>(() => definition.Update(
            "nutrition",
            AchievementMetric.TotalMeals,
            threshold: 20,
            "Новое название",
            "New title",
            "Новое описание",
            "New description",
            "restaurant",
            sortOrder: 2,
            isActive: false));
        Assert.Equal("habits", definition.Category);
        Assert.Equal(10, definition.Threshold);
        Assert.Equal(int.MaxValue, definition.Version);
        Assert.Null(definition.ModifiedOnUtc);
    }
}
