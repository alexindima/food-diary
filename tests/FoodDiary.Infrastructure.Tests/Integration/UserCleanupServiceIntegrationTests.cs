using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recents;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Application.Images.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

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

        var storage = new RecordingImageStorageService();
        var service = new UserCleanupService(context, storage, NullLogger<UserCleanupService>.Instance);

        var removed = await service.CleanupDeletedUsersAsync(DateTime.UtcNow.AddDays(-1), batchSize: 10, reassignUserId: null);

        await using var verificationContext = CreateVerificationContext(context);

        Assert.Equal(1, removed);
        Assert.False(await verificationContext.Users.AnyAsync(user => user.Id == deletedUser.Id));
        Assert.False(await verificationContext.Products.AnyAsync());
        Assert.False(await verificationContext.Recipes.AnyAsync());
        Assert.False(await verificationContext.RecipeSteps.AnyAsync());
        Assert.False(await verificationContext.ImageAssets.AnyAsync());
        Assert.False(await verificationContext.ShoppingLists.AnyAsync());
        Assert.False(await verificationContext.ShoppingListItems.AnyAsync());
        Assert.False(await verificationContext.RecentItems.AnyAsync());
        Assert.False(await verificationContext.AiUsages.AnyAsync());
        Assert.Equal(["users/deleted/image-1.webp"], storage.DeletedObjectKeys);
    }

    [RequiresDockerFact]
    public async Task CleanupDeletedUsersAsync_WithReassign_ReassignsContentAssetsAndDeletesUser() {
        await using var context = await databaseFixture.CreateDbContextAsync();
        var deletedUser = User.Create("deleted@example.com", "hash");
        deletedUser.MarkDeleted(DateTime.UtcNow.AddDays(-10));
        var survivorUser = User.Create("survivor@example.com", "hash");
        survivorUser.UpdateProfileMedia(profileImageAssetId: null);

        var productAsset = ImageAsset.Create(deletedUser.Id, "users/deleted/product.webp", "https://cdn.example.com/product.webp");
        var recipeAsset = ImageAsset.Create(deletedUser.Id, "users/deleted/recipe.webp", "https://cdn.example.com/recipe.webp");
        var stepAsset = ImageAsset.Create(deletedUser.Id, "users/deleted/step.webp", "https://cdn.example.com/step.webp");
        var profileAsset = ImageAsset.Create(deletedUser.Id, "users/deleted/profile.webp", "https://cdn.example.com/profile.webp");
        var mealAsset = ImageAsset.Create(deletedUser.Id, "users/deleted/meal.webp", "https://cdn.example.com/meal.webp");

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
        var meal = FoodDiary.Domain.Entities.Meals.Meal.Create(deletedUser.Id, new DateTime(2026, 3, 29, 0, 0, 0, DateTimeKind.Utc), imageAssetId: mealAsset.Id);
        var shoppingList = ShoppingList.Create(deletedUser.Id, "Temporary");
        shoppingList.AddItem("Bread", product.Id, 2, MeasurementUnit.Pcs, "Bakery", false, 0);
        var recentItem = RecentItem.Create(deletedUser.Id, RecentItemType.Recipe, recipe.Id.Value);
        var aiUsage = AiUsage.Create(deletedUser.Id, "nutrition", "gpt-4.1-mini", 15, 25, 40);
        deletedUser.UpdateProfileMedia(profileImageAssetId: profileAsset.Id);

        context.Users.AddRange(deletedUser, survivorUser);
        context.ImageAssets.AddRange(productAsset, recipeAsset, stepAsset, profileAsset, mealAsset);
        context.Products.Add(product);
        context.Recipes.Add(recipe);
        context.Meals.Add(meal);
        context.ShoppingLists.Add(shoppingList);
        context.RecentItems.Add(recentItem);
        context.AiUsages.Add(aiUsage);
        await context.SaveChangesAsync();

        var storage = new RecordingImageStorageService();
        var service = new UserCleanupService(context, storage, NullLogger<UserCleanupService>.Instance);

        var removed = await service.CleanupDeletedUsersAsync(
            DateTime.UtcNow.AddDays(-1),
            batchSize: 10,
            reassignUserId: survivorUser.Id.Value);

        await using var verificationContext = CreateVerificationContext(context);

        Assert.Equal(1, removed);
        Assert.False(await verificationContext.Users.AnyAsync(user => user.Id == deletedUser.Id));
        Assert.True(await verificationContext.Users.AnyAsync(user => user.Id == survivorUser.Id));

        var reassignedProduct = await verificationContext.Products.SingleAsync();
        var reassignedRecipe = await verificationContext.Recipes.SingleAsync();
        var reassignedAssets = await verificationContext.ImageAssets.OrderBy(asset => asset.ObjectKey).ToListAsync();

        Assert.Equal(survivorUser.Id, reassignedProduct.UserId);
        Assert.Equal(survivorUser.Id, reassignedRecipe.UserId);
        Assert.All(reassignedAssets, asset => Assert.Equal(survivorUser.Id, asset.UserId));
        Assert.Single(await verificationContext.RecipeSteps.ToListAsync());
        Assert.False(await verificationContext.Meals.AnyAsync());
        Assert.False(await verificationContext.ShoppingLists.AnyAsync());
        Assert.False(await verificationContext.ShoppingListItems.AnyAsync());
        Assert.False(await verificationContext.RecentItems.AnyAsync());
        Assert.False(await verificationContext.AiUsages.AnyAsync());
        Assert.Equal(
            ["users/deleted/meal.webp", "users/deleted/profile.webp"],
            storage.DeletedObjectKeys.OrderBy(static key => key).ToArray());
    }

    private static FoodDiaryDbContext CreateVerificationContext(FoodDiaryDbContext sourceContext) {
        var connectionString = sourceContext.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Source context does not have a connection string.");

        var options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseNpgsql(new NpgsqlConnectionStringBuilder(connectionString).ConnectionString)
            .Options;

        return new FoodDiaryDbContext(options);
    }

    private sealed class RecordingImageStorageService : IImageStorageService {
        public List<string> DeletedObjectKeys { get; } = [];

        public Task<PresignedUpload> CreatePresignedUploadAsync(
            UserId userId,
            string fileName,
            string contentType,
            long fileSizeBytes,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task DeleteAsync(string objectKey, CancellationToken cancellationToken) {
            DeletedObjectKeys.Add(objectKey);
            return Task.CompletedTask;
        }
    }
}
