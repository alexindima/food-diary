using FoodDiary.Domain.Entities.MealPlans;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class MealPlanRecipeSnapshotTests {
    [Fact]
    public void SnapshotRejectsAnotherRecipeAndCopiesIngredientCollection() {
        var recipeId = RecipeId.New();
        MealPlanMeal meal = MealPlan.CreateCurated("Plan", description: null, DietType.Balanced, 1, targetCaloriesPerDay: null)
            .AddDay(1).AddMeal(MealType.Lunch, recipeId);
        List<MealPlanRecipeIngredientSnapshot> ingredients = [new(ProductId.New(), 100, "Rice", MeasurementUnit.G, "Pantry")];
        var snapshot = new MealPlanRecipeSnapshot(recipeId, "Rice bowl", 2, ingredients);
        meal.SetRecipeSnapshot(snapshot);
        ingredients.Clear();
        Assert.Single(meal.RecipeSnapshot!.Ingredients);
        Assert.Throws<ArgumentException>(() => meal.SetRecipeSnapshot(snapshot with { Id = RecipeId.New() }));
        Assert.Equal(recipeId, meal.RecipeSnapshot.Id);
        meal.SetRecipeSnapshot(snapshot: null);
        Assert.Null(meal.RecipeSnapshot);
    }
}
