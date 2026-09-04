using FoodDiary.Application.Abstractions.FavoriteMeals.Common;
using FoodDiary.Application.Abstractions.FavoriteProducts.Common;
using FoodDiary.Application.Abstractions.FavoriteRecipes.Common;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class FeatureErrorContractTests {
    [Fact]
    public void FavoriteMealErrors_HasOwnerAssemblyAndNamespace() {
        Assert.Multiple(
            () => Assert.Equal("FoodDiary.Modules.Favorites.Application.Abstractions", typeof(FavoriteMealErrors).Assembly.GetName().Name),
            () => Assert.Equal("FoodDiary.Application.Abstractions.FavoriteMeals.Common", typeof(FavoriteMealErrors).Namespace));
    }

    [Fact]
    public void FavoriteMealErrors_PreservesEveryPublicErrorContract() {
        AssertError(FavoriteMealErrors.NotFound(Guid.Parse("12345678-1234-1234-1234-123456789abc")), "FavoriteMeal.NotFound", "Favorite meal with id '12345678-1234-1234-1234-123456789abc' was not found.", ErrorKind.NotFound);
        AssertError(FavoriteMealErrors.AlreadyExists, "FavoriteMeal.AlreadyExists", "This meal is already in favorites.", ErrorKind.Conflict);
    }

    [Fact]
    public void FavoriteProductErrors_HasOwnerAssemblyAndNamespace() {
        Assert.Multiple(
            () => Assert.Equal("FoodDiary.Modules.Favorites.Application.Abstractions", typeof(FavoriteProductErrors).Assembly.GetName().Name),
            () => Assert.Equal("FoodDiary.Application.Abstractions.FavoriteProducts.Common", typeof(FavoriteProductErrors).Namespace));
    }

    [Fact]
    public void FavoriteProductErrors_PreservesEveryPublicErrorContract() {
        AssertError(FavoriteProductErrors.NotFound(Guid.Parse("12345678-1234-1234-1234-123456789abc")), "FavoriteProduct.NotFound", "Favorite product with id '12345678-1234-1234-1234-123456789abc' was not found.", ErrorKind.NotFound);
        AssertError(FavoriteProductErrors.AlreadyExists, "FavoriteProduct.AlreadyExists", "This product is already in favorites.", ErrorKind.Conflict);
    }

    [Fact]
    public void FavoriteRecipeErrors_HasOwnerAssemblyAndNamespace() {
        Assert.Multiple(
            () => Assert.Equal("FoodDiary.Modules.Favorites.Application.Abstractions", typeof(FavoriteRecipeErrors).Assembly.GetName().Name),
            () => Assert.Equal("FoodDiary.Application.Abstractions.FavoriteRecipes.Common", typeof(FavoriteRecipeErrors).Namespace));
    }

    [Fact]
    public void FavoriteRecipeErrors_PreservesEveryPublicErrorContract() {
        AssertError(FavoriteRecipeErrors.NotFound(Guid.Parse("12345678-1234-1234-1234-123456789abc")), "FavoriteRecipe.NotFound", "Favorite recipe with id '12345678-1234-1234-1234-123456789abc' was not found.", ErrorKind.NotFound);
        AssertError(FavoriteRecipeErrors.AlreadyExists, "FavoriteRecipe.AlreadyExists", "This recipe is already in favorites.", ErrorKind.Conflict);
    }

    private static void AssertError(Error error, string code, string message, ErrorKind kind) {
        Assert.Multiple(
            () => Assert.Equal(code, error.Code),
            () => Assert.Equal(message, error.Message),
            () => Assert.Equal(kind, error.Kind),
            () => Assert.Null(error.Details));
    }
}
