using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Recipes.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class RecipeGalleryTests {
    [Fact]
    public void StepGallery_PreservesOrderAndEnforcesLimit() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2);
        RecipeStep step = recipe.AddStep(1, "Cook");
        RecipeImage[] images = [.. Enumerable.Range(0, 5).Select(index => new RecipeImage(ImageAssetId.New(), "https://example.test/photo.jpg", index))];
        step.ReplaceImages(images);
        Assert.Equal(5, step.Images.Count);
        step.ReplaceImages([images[4], images[0]]);
        Assert.Equal(images[4].ImageAssetId, step.ImageAssetId);
        Assert.Equal(2, step.Images.Count);
        Assert.Throws<ArgumentException>(() => step.ReplaceImages([images[0], images[0]]));
        Assert.Throws<ArgumentException>(() => step.ReplaceImages([.. images, new RecipeImage(ImageAssetId.New(), "https://example.test/extra.jpg", 5)]));
        step.ReplaceImages([]);
        Assert.Empty(step.Images);
        Assert.Null(step.ImageUrl);
    }

    [Fact]
    public void ReplaceImages_ReordersCoverAndClearsAll() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2);
        var first = new RecipeImage(ImageAssetId.New(), "https://example.test/first.jpg", 0);
        var second = new RecipeImage(ImageAssetId.New(), "https://example.test/second.jpg", 1);
        recipe.ReplaceImages([first, second]);
        recipe.ReplaceImages([second, first]);
        Assert.Equal(second.ImageAssetId, recipe.ImageAssetId);
        Assert.Equal(second.ImageUrl, recipe.ImageUrl);
        Assert.Equal(second.ImageAssetId, recipe.Images.OrderBy(image => image.Position).First().ImageAssetId);
        recipe.ReplaceImages([]);
        Assert.Empty(recipe.Images);
        Assert.Null(recipe.ImageAssetId);
        Assert.Null(recipe.ImageUrl);
    }

    [Fact]
    public void ReplaceImages_RejectsDuplicatesAndMoreThanFive() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2);
        var image = new RecipeImage(ImageAssetId.New(), "https://example.test/photo.jpg", 0);
        Assert.Throws<ArgumentException>(() => recipe.ReplaceImages([image, image]));
        RecipeImage[] tooMany = [.. Enumerable.Range(0, 6).Select(index => new RecipeImage(ImageAssetId.New(), "https://example.test/photo.jpg", index))];
        Assert.Throws<ArgumentException>(() => recipe.ReplaceImages(tooMany));
    }
}
