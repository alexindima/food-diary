using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recents;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Infrastructure.Tests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
public sealed class UserCleanupServiceIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task CleanupDeletedUsersAsync_WithoutReassign_RemovesUserAndOwnedData() {
        await using var context = await databaseFixture.CreateDbContextAsync();
        var deletedUser = User.Create("deleted@example.com", "hash");
        deletedUser.MarkDeleted(DateTime.UtcNow.AddDays(-10));

        var imageAsset = ImageAsset.Create(deletedUser.Id, "users/deleted/image-1.webp", "https://cdn.example.com/image-1.webp");
        var product = Product.Create(
            deletedUser.Id,
            "Apple",
            MeasurementUnit.G,
            100,
            100,
            52,
            0.3,
            0.2,
            14,
            2.4,
            0,
            imageAssetId: imageAsset.Id);
        var recipe = Recipe.Create(
            deletedUser.Id,
            "Pie",
            servings: 2,
            imageAssetId: imageAsset.Id,
            visibility: Visibility.Private);
        recipe.AddStep(1, "Mix ingredients", imageAssetId: imageAsset.Id);
        var shoppingList = ShoppingList.Create(deletedUser.Id, "Cleanup");
        shoppingList.AddItem("Apple", product.Id, 1, MeasurementUnit.Pcs, "Fruit", false, 0);
        var recentItem = RecentItem.Create(deletedUser.Id, RecentItemType.Product, product.Id.Value);
        var aiUsage = AiUsage.Create(deletedUser.Id, "vision", "gpt-4.1-mini", 10, 20, 30);

        context.Users.Add(deletedUser);
        context.ImageAssets.Add(imageAsset);
        context.Products.Add(product);
        context.Recipes.Add(recipe);
        context.ShoppingLists.Add(shoppingList);
        context.RecentItems.Add(recentItem);
        context.AiUsages.Add(aiUsage);
        await context.SaveChangesAsync();

        var service = new UserCleanupService(context, NullLogger<UserCleanupService>.Instance);

        var removed = await service.CleanupDeletedUsersAsync(DateTime.UtcNow.AddDays(-1), batchSize: 10, reassignUserId: null);

        Assert.Equal(1, removed);
        Assert.False(await context.Users.AnyAsync());
        Assert.False(await context.Products.AnyAsync());
        Assert.False(await context.Recipes.AnyAsync());
        Assert.False(await context.RecipeSteps.AnyAsync());
        Assert.False(await context.ImageAssets.AnyAsync());
        Assert.False(await context.ShoppingLists.AnyAsync());
        Assert.False(await context.ShoppingListItems.AnyAsync());
        Assert.False(await context.RecentItems.AnyAsync());
        Assert.False(await context.AiUsages.AnyAsync());
    }

    [RequiresDockerFact]
    public async Task CleanupDeletedUsersAsync_WithReassign_ReassignsContentAssetsAndDeletesUser() {
        await using var context = await databaseFixture.CreateDbContextAsync();
        var deletedUser = User.Create("deleted@example.com", "hash");
        deletedUser.MarkDeleted(DateTime.UtcNow.AddDays(-10));
        var survivorUser = User.Create("survivor@example.com", "hash");

        var productAsset = ImageAsset.Create(deletedUser.Id, "users/deleted/product.webp", "https://cdn.example.com/product.webp");
        var recipeAsset = ImageAsset.Create(deletedUser.Id, "users/deleted/recipe.webp", "https://cdn.example.com/recipe.webp");
        var stepAsset = ImageAsset.Create(deletedUser.Id, "users/deleted/step.webp", "https://cdn.example.com/step.webp");

        var product = Product.Create(
            deletedUser.Id,
            "Bread",
            MeasurementUnit.G,
            100,
            100,
            265,
            9,
            3.2,
            49,
            2.7,
            0,
            imageAssetId: productAsset.Id);
        var recipe = Recipe.Create(
            deletedUser.Id,
            "Toast",
            servings: 1,
            imageAssetId: recipeAsset.Id);
        recipe.AddStep(1, "Toast bread", imageAssetId: stepAsset.Id);
        var shoppingList = ShoppingList.Create(deletedUser.Id, "Temporary");
        shoppingList.AddItem("Bread", product.Id, 2, MeasurementUnit.Pcs, "Bakery", false, 0);
        var recentItem = RecentItem.Create(deletedUser.Id, RecentItemType.Recipe, recipe.Id.Value);
        var aiUsage = AiUsage.Create(deletedUser.Id, "nutrition", "gpt-4.1-mini", 15, 25, 40);

        context.Users.AddRange(deletedUser, survivorUser);
        context.ImageAssets.AddRange(productAsset, recipeAsset, stepAsset);
        context.Products.Add(product);
        context.Recipes.Add(recipe);
        context.ShoppingLists.Add(shoppingList);
        context.RecentItems.Add(recentItem);
        context.AiUsages.Add(aiUsage);
        await context.SaveChangesAsync();

        var service = new UserCleanupService(context, NullLogger<UserCleanupService>.Instance);

        var removed = await service.CleanupDeletedUsersAsync(
            DateTime.UtcNow.AddDays(-1),
            batchSize: 10,
            reassignUserId: survivorUser.Id.Value);

        Assert.Equal(1, removed);
        Assert.False(await context.Users.AnyAsync(user => user.Id == deletedUser.Id));
        Assert.Equal(1, await context.Users.CountAsync(user => user.Id == survivorUser.Id));

        var reassignedProduct = await context.Products.SingleAsync();
        var reassignedRecipe = await context.Recipes.SingleAsync();
        var reassignedAssets = await context.ImageAssets.OrderBy(asset => asset.ObjectKey).ToListAsync();

        Assert.Equal(survivorUser.Id, reassignedProduct.UserId);
        Assert.Equal(survivorUser.Id, reassignedRecipe.UserId);
        Assert.All(reassignedAssets, asset => Assert.Equal(survivorUser.Id, asset.UserId));
        Assert.Single(await context.RecipeSteps.ToListAsync());
        Assert.False(await context.ShoppingLists.AnyAsync());
        Assert.False(await context.ShoppingListItems.AnyAsync());
        Assert.False(await context.RecentItems.AnyAsync());
        Assert.False(await context.AiUsages.AnyAsync());
    }
}
