using FoodDiary.Modules.Meals.Domain.Entities;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Meals.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class TypedMealQuantityTests {
    [Fact]
    public void QuantityMutation_RejectsTheOppositeItemKindWithoutChangingState() {
        var meal = Meal.Create(UserId.New(), new DateTime(2026, 10, 8, 9, 0, 0, DateTimeKind.Utc));
        MealItem product = meal.AddProduct(ProductId.New(), ProductUnitQuantity.FromUnits(125.5));
        MealItem recipe = meal.AddRecipe(RecipeId.New(), RecipeServingQuantity.FromServings(0.75));

        Assert.Throws<InvalidOperationException>(() => product.UpdateRecipeServings(RecipeServingQuantity.FromServings(2)));
        Assert.Throws<InvalidOperationException>(() => recipe.UpdateProductQuantity(ProductUnitQuantity.FromUnits(250)));

        Assert.Multiple(() => {
            Assert.Equal(125.5, product.Amount);
            Assert.Equal(0.75, recipe.Amount);
            Assert.Null(product.ModifiedOnUtc);
            Assert.Null(recipe.ModifiedOnUtc);
        });
    }

    [Fact]
    public void ServingMutation_PreservesFractionalValueAndNoOpAudit() {
        var meal = Meal.Create(UserId.New(), new DateTime(2026, 10, 8, 9, 0, 0, DateTimeKind.Utc));
        MealItem item = meal.AddRecipe(RecipeId.New(), RecipeServingQuantity.FromServings(0.75));
        item.UpdateRecipeServings(RecipeServingQuantity.FromServings(0.75));
        Assert.Null(item.ModifiedOnUtc);
        item.UpdateRecipeServings(RecipeServingQuantity.FromServings(1.125));
        Assert.Equal(1.125, item.Amount);
        Assert.NotNull(item.ModifiedOnUtc);
    }

    [Fact]
    public void MissingQuantity_IsRejectedAtTheTypedBoundary() {
        var meal = Meal.Create(UserId.New(), new DateTime(2026, 10, 8, 9, 0, 0, DateTimeKind.Utc));
        Assert.Throws<ArgumentNullException>(() => meal.AddProduct(ProductId.New(), null!));
        Assert.Throws<ArgumentNullException>(() => meal.AddRecipe(RecipeId.New(), null!));
        Assert.Empty(meal.Items);
    }
}
