using FoodDiary.Modules.Favorites.Domain.Entities.FavoriteMeals;
using FoodDiary.Modules.Favorites.Domain.Entities.FavoriteProducts;
using FoodDiary.Modules.Favorites.Domain.Entities.FavoriteRecipes;
using FoodDiary.Modules.Meals.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Favorites.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class FavoriteDisplayNameBoundaryTests {

    [Fact]
    public void LinkAndUserFacingValues_RejectInvalidInput() {
        string longName = new('n', 2048 + 1);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FavoriteProduct.Create(UserId.New(), ProductId.New(), longName));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FavoriteMeal.Create(UserId.New(), MealId.New(), longName));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FavoriteRecipe.Create(UserId.New(), RecipeId.New(), longName));
    }
}
