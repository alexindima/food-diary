using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Meals.Domain.Contracts.Enums;
using FoodDiary.Modules.MealPlanning.Domain.Enums;
using FoodDiary.Modules.MealPlanning.Domain.Entities.MealPlans;

namespace FoodDiary.Modules.MealPlanning.Domain.Tests;

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
