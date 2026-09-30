using FoodDiary.Modules.MealPlanning.Domain.Entities.MealPlans;
using FoodDiary.Modules.MealPlanning.Domain.Enums;
using FoodDiary.Modules.Meals.Domain.Contracts.Enums;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.MealPlanning.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class CatalogMealPlanTests {
    [Fact]
    public void EditingAndHidingTemplate_PreservesAdoptedCopy() {
        var plan = MealPlan.CreateCurated("Original", description: null, DietType.Balanced, 1, 1800);
        var recipeId = RecipeId.New();
        plan.AddDay(1).AddMeal(MealType.Lunch, recipeId, servings: 2);
        MealPlan adopted = plan.Adopt(UserId.New());

        plan.UpdateCatalogDetails("Changed", "Draft", DietType.Vegan, 2, 2100, isPublished: false);
        plan.AddDay(2).AddMeal(MealType.Dinner, RecipeId.New(), servings: 4);

        Assert.Null(plan.UserId);
        Assert.False(plan.IsCurated);
        Assert.Equal("Original", adopted.Name);
        Assert.Equal(1, adopted.DurationDays);
        MealPlanMeal meal = Assert.Single(Assert.Single(adopted.Days).Meals);
        Assert.Equal(recipeId, meal.RecipeId);
        Assert.Equal(2, meal.Servings);
        Assert.NotEqual(plan.Id, adopted.Id);
    }

    [Fact]
    public void CatalogEdit_RejectsUserOwnedPlan() {
        var plan = MealPlan.CreateForUser(UserId.New(), "Personal", description: null, DietType.Balanced, 1, targetCaloriesPerDay: null);
        Assert.Throws<InvalidOperationException>(() => plan.UpdateCatalogDetails("Overwrite", description: null, DietType.Keto, 1, targetCaloriesPerDay: null, isPublished: true));
        Assert.Equal("Personal", plan.Name);
    }
}
