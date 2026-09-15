using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Recipes.Domain.Events;

namespace FoodDiary.Modules.Recipes.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class RecipeNutritionEventPayloadTests {
    [Fact]
    public void EventProperties_ExposeConstructorValues() {
        var recipeId = RecipeId.New();
        var occurredOnUtc = new DateTime(2026, 7, 8, 10, 0, 0, DateTimeKind.Utc);
        var autoNutrition = new RecipeAutoNutritionEnabledDomainEvent(recipeId, occurredOnUtc);
        var manualNutrition = new RecipeManualNutritionSetDomainEvent(recipeId, occurredOnUtc);
        Assert.Multiple(
            () => Assert.Equal(recipeId, autoNutrition.RecipeId),
            () => Assert.Equal(occurredOnUtc, autoNutrition.OccurredOnUtc),
            () => Assert.Equal(recipeId, manualNutrition.RecipeId),
            () => Assert.Equal(occurredOnUtc, manualNutrition.OccurredOnUtc));
    }
}
