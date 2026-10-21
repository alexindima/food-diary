using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Recipes.Domain.Contracts.Enums;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Recipes.Domain.ValueObjects;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Recipes.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class RecipeFieldChangesTests {
    [Fact]
    public void IdentityChanges_DistinguishOmittedSetAndClearedFields() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2, description: "Original", comment: "Keep", category: "soups");

        recipe.UpdateIdentityChanges(new RecipeIdentityChanges(Name: null, FieldChanges.Unchanged<string>(),
            FieldChanges.Unchanged<string>(), FieldChanges.Unchanged<string>()));
        Assert.Null(recipe.ModifiedOnUtc);

        recipe.UpdateIdentityChanges(new RecipeIdentityChanges("  Salad  ", FieldChanges.Set("  Fresh  "),
            FieldChanges.Unchanged<string>(), FieldChanges.Set("salads")));
        Assert.Multiple(
            () => Assert.Equal("Salad", recipe.Name),
            () => Assert.Equal("Fresh", recipe.Description),
            () => Assert.Equal("Keep", recipe.Comment),
            () => Assert.Equal(RecipeCategory.Salads, recipe.Category),
            () => Assert.NotNull(recipe.ModifiedOnUtc));

        recipe.UpdateIdentityChanges(new RecipeIdentityChanges(Name: null, FieldChanges.Clear<string>(),
            FieldChanges.Clear<string>(), FieldChanges.Clear<string>()));
        Assert.Multiple(
            () => Assert.Null(recipe.Description),
            () => Assert.Null(recipe.Comment),
            () => Assert.Equal(RecipeCategory.Other, recipe.Category));
    }

    [Fact]
    public void MediaChanges_PreserveOmittedValuesAndApplyExplicitClear() {
        var originalId = ImageAssetId.New();
        var replacementId = ImageAssetId.New();
        var recipe = Recipe.Create(UserId.New(), "Soup", 2, imageUrl: "https://example.org/original", imageAssetId: originalId);
        recipe.UpdateMediaChanges(new RecipeMediaChanges(FieldChanges.Unchanged<string>(), FieldChanges.Unchanged<ImageAssetId>()));
        Assert.Null(recipe.ModifiedOnUtc);

        recipe.UpdateMediaChanges(new RecipeMediaChanges(FieldChanges.Set(" https://example.org/replacement "), FieldChanges.Set(replacementId)));
        Assert.Multiple(
            () => Assert.Equal("https://example.org/replacement", recipe.ImageUrl),
            () => Assert.Equal(replacementId, recipe.ImageAssetId));

        recipe.UpdateMediaChanges(new RecipeMediaChanges(FieldChanges.Clear<string>(), FieldChanges.Clear<ImageAssetId>()));
        Assert.Multiple(() => Assert.Null(recipe.ImageUrl), () => Assert.Null(recipe.ImageAssetId));
    }

    [Theory]
    [InlineData("description")]
    [InlineData("comment")]
    [InlineData("category")]
    public void IdentityChanges_RejectNullFieldChangesBeforeMutatingTheRecipe(string field) {
        var recipe = Recipe.Create(UserId.New(), "Original", 2);
        var changes = new RecipeIdentityChanges("Changed", FieldChanges.Unchanged<string>(),
            FieldChanges.Unchanged<string>(), FieldChanges.Unchanged<string>());
        changes = field switch {
            "description" => changes with { Description = null! },
            "comment" => changes with { Comment = null! },
            "category" => changes with { Category = null! },
            _ => throw new ArgumentOutOfRangeException(nameof(field)),
        };

        Assert.Throws<ArgumentNullException>(() => recipe.UpdateIdentityChanges(changes));
        Assert.Multiple(() => Assert.Equal("Original", recipe.Name), () => Assert.Null(recipe.ModifiedOnUtc));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MediaChanges_RejectNullFieldChangesBeforeMutatingTheRecipe(bool missingUrl) {
        var id = ImageAssetId.New();
        var recipe = Recipe.Create(UserId.New(), "Soup", 2, imageUrl: "https://example.org/original", imageAssetId: id);
        var changes = new RecipeMediaChanges(missingUrl ? null! : FieldChanges.Set("https://example.org/replacement"),
            missingUrl ? FieldChanges.Clear<ImageAssetId>() : null!);

        Assert.Throws<ArgumentNullException>(() => recipe.UpdateMediaChanges(changes));
        Assert.Multiple(
            () => Assert.Equal("https://example.org/original", recipe.ImageUrl),
            () => Assert.Equal(id, recipe.ImageAssetId),
            () => Assert.Null(recipe.ModifiedOnUtc));
    }

    [Fact]
    public void CompositeUpdate_CanBeCopiedWithNewValuesAndExplicitClears() {
        var recipe = Recipe.Create(UserId.New(), "Original", 1);
        var imageId = ImageAssetId.New();
        RecipeUpdate original = new();
        RecipeUpdate update = original with {
            Name = "Updated",
            Description = "Description",
            Comment = "Comment",
            Category = "salads",
            ImageUrl = "https://example.org/image",
            ImageAssetId = imageId,
            PrepTime = 5,
            CookTime = 10,
            Servings = 3,
            Visibility = Visibility.Private,
        };
        recipe.Update(update);

        Assert.Multiple(
            () => Assert.Equal(default, original),
            () => Assert.Equal("Updated", recipe.Name),
            () => Assert.Equal("Description", recipe.Description),
            () => Assert.Equal("Comment", recipe.Comment),
            () => Assert.Equal(RecipeCategory.Salads, recipe.Category),
            () => Assert.Equal("https://example.org/image", recipe.ImageUrl),
            () => Assert.Equal(imageId, recipe.ImageAssetId),
            () => Assert.Equal(5, recipe.PrepTime),
            () => Assert.Equal(10, recipe.CookTime),
            () => Assert.Equal(3, recipe.Servings),
            () => Assert.Equal(Visibility.Private, recipe.Visibility));

        recipe.Update(original with {
            ClearDescription = true,
            ClearComment = true,
            ClearCategory = true,
            ClearImageUrl = true,
            ClearImageAssetId = true,
            ClearPrepTime = true,
            ClearCookTime = true,
        });
        Assert.Multiple(
            () => Assert.Null(recipe.Description),
            () => Assert.Null(recipe.Comment),
            () => Assert.Equal(RecipeCategory.Other, recipe.Category),
            () => Assert.Null(recipe.ImageUrl),
            () => Assert.Null(recipe.ImageAssetId),
            () => Assert.Null(recipe.PrepTime),
            () => Assert.Null(recipe.CookTime),
            () => Assert.Equal(3, recipe.Servings),
            () => Assert.Equal(Visibility.Private, recipe.Visibility));
    }
}
