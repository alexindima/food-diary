using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.FavoriteProducts;
using FoodDiary.Domain.Entities.FavoriteRecipes;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Domain;

[ExcludeFromCodeCoverage]
public class FavoriteInvariantTests {
    [Fact]
    public void FavoriteProduct_Create_WithValidValues_NormalizesNameAndSetsTimestamps() {
        var userId = UserId.New();
        var productId = ProductId.New();

        var favorite = FavoriteProduct.Create(userId, productId, "  Apple  ");

        Assert.NotEqual(FavoriteProductId.Empty, favorite.Id);
        Assert.Equal(userId, favorite.UserId);
        Assert.Equal(productId, favorite.ProductId);
        Assert.Equal("Apple", favorite.Name);
        Assert.NotEqual(default, favorite.CreatedAtUtc);
        Assert.NotEqual(default, favorite.CreatedOnUtc);
    }

    [Fact]
    public void FavoriteProduct_Create_WithBlankName_StoresNull() {
        var favorite = FavoriteProduct.Create(UserId.New(), ProductId.New(), " ");

        Assert.Null(favorite.Name);
    }

    [Fact]
    public void FavoriteProduct_Create_WithEmptyIds_Throws() {
        Assert.Throws<ArgumentException>(() => FavoriteProduct.Create(UserId.Empty, ProductId.New()));
        Assert.Throws<ArgumentException>(() => FavoriteProduct.Create(UserId.New(), ProductId.Empty));
    }

    [Fact]
    public void FavoriteProduct_Create_WithTooLongName_TruncatesToCommentMaxLength() {
        var favorite = FavoriteProduct.Create(UserId.New(), ProductId.New(), new string('n', DomainConstants.CommentMaxLength + 1));

        Assert.Equal(DomainConstants.CommentMaxLength, favorite.Name!.Length);
    }

    [Fact]
    public void FavoriteProduct_UpdateName_NormalizesAndAvoidsUnchangedUpdates() {
        var favorite = FavoriteProduct.Create(UserId.New(), ProductId.New(), "Apple");

        favorite.UpdateName("  Apple  ");
        Assert.Null(favorite.ModifiedOnUtc);

        favorite.UpdateName("  Banana  ");
        Assert.Equal("Banana", favorite.Name);
        Assert.NotNull(favorite.ModifiedOnUtc);
    }

    [Fact]
    public void FavoriteProduct_UpdateName_WithBlankName_ClearsName() {
        var favorite = FavoriteProduct.Create(UserId.New(), ProductId.New(), "Apple");

        favorite.UpdateName(" ");

        Assert.Null(favorite.Name);
        Assert.NotNull(favorite.ModifiedOnUtc);
    }

    [Fact]
    public void FavoriteRecipe_Create_WithValidValues_NormalizesNameAndSetsTimestamps() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();

        var favorite = FavoriteRecipe.Create(userId, recipeId, "  Soup  ");

        Assert.NotEqual(FavoriteRecipeId.Empty, favorite.Id);
        Assert.Equal(userId, favorite.UserId);
        Assert.Equal(recipeId, favorite.RecipeId);
        Assert.Equal("Soup", favorite.Name);
        Assert.NotEqual(default, favorite.CreatedAtUtc);
        Assert.NotEqual(default, favorite.CreatedOnUtc);
    }

    [Fact]
    public void FavoriteRecipe_Create_WithBlankName_StoresNull() {
        var favorite = FavoriteRecipe.Create(UserId.New(), RecipeId.New(), " ");

        Assert.Null(favorite.Name);
    }

    [Fact]
    public void FavoriteRecipe_Create_WithEmptyIds_Throws() {
        Assert.Throws<ArgumentException>(() => FavoriteRecipe.Create(UserId.Empty, RecipeId.New()));
        Assert.Throws<ArgumentException>(() => FavoriteRecipe.Create(UserId.New(), RecipeId.Empty));
    }

    [Fact]
    public void FavoriteRecipe_Create_WithTooLongName_TruncatesToCommentMaxLength() {
        var favorite = FavoriteRecipe.Create(UserId.New(), RecipeId.New(), new string('n', DomainConstants.CommentMaxLength + 1));

        Assert.Equal(DomainConstants.CommentMaxLength, favorite.Name!.Length);
    }

    [Fact]
    public void FavoriteRecipe_UpdateName_NormalizesAndAvoidsUnchangedUpdates() {
        var favorite = FavoriteRecipe.Create(UserId.New(), RecipeId.New(), "Soup");

        favorite.UpdateName("  Soup  ");
        Assert.Null(favorite.ModifiedOnUtc);

        favorite.UpdateName("  Stew  ");
        Assert.Equal("Stew", favorite.Name);
        Assert.NotNull(favorite.ModifiedOnUtc);
    }

    [Fact]
    public void FavoriteRecipe_UpdateName_WithBlankName_ClearsName() {
        var favorite = FavoriteRecipe.Create(UserId.New(), RecipeId.New(), "Soup");

        favorite.UpdateName(" ");

        Assert.Null(favorite.Name);
        Assert.NotNull(favorite.ModifiedOnUtc);
    }
}
