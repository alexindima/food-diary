using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.Entities.Admin;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recents;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class UserCleanupServiceIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task CleanupDeletedUsersAsync_WithoutReassign_RemovesUserAndOwnedData() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var deletedUser = User.Create("deleted@example.com", "hash");
        var survivingUser = User.Create("survivor@example.com", "hash");
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
        shoppingList.AddItem("Apple", product.Id, 1, MeasurementUnit.Pcs, "Fruit", isChecked: false, 0);
        var recentItem = RecentItem.Create(deletedUser.Id, RecentItemType.Product, product.Id.Value);
        var aiUsage = AiUsage.Create(deletedUser.Id, "vision", "gpt-4.1-mini", 10, 20, 30);
        var recordedAt = new DateTime(2026, 7, 26, 8, 0, 0, DateTimeKind.Utc);
        var meal = FoodDiary.Domain.Entities.Meals.Meal.Create(deletedUser.Id, recordedAt);
        var hydration = HydrationEntry.Create(deletedUser.Id, recordedAt, 250);
        var weight = WeightEntry.Create(deletedUser.Id, recordedAt, 72.5);
        var waist = WaistEntry.Create(deletedUser.Id, recordedAt, 84);
        var targetedSession = AdminImpersonationSession.Start(
            survivingUser.Id,
            deletedUser.Id,
            "Investigate account support request",
            actorIpAddress: null,
            actorUserAgent: null,
            recordedAt);
        var actorSession = AdminImpersonationSession.Start(
            deletedUser.Id,
            survivingUser.Id,
            "Investigate account support request",
            actorIpAddress: null,
            actorUserAgent: null,
            recordedAt);
        var assignedTask = ClientTask.Create(survivingUser.Id, deletedUser.Id, "Review plan", details: null, dueAtUtc: null);
        var authoredTask = ClientTask.Create(deletedUser.Id, survivingUser.Id, "Review plan", details: null, dueAtUtc: null);

        context.Users.AddRange(deletedUser, survivingUser);
        context.AdminImpersonationSessions.AddRange(targetedSession, actorSession);
        context.ClientTasks.AddRange(assignedTask, authoredTask);
        context.ImageAssets.Add(imageAsset);
        context.Products.Add(product);
        context.Recipes.Add(recipe);
        context.ShoppingLists.Add(shoppingList);
        context.RecentItems.Add(recentItem);
        context.AiUsages.Add(aiUsage);
        context.Meals.Add(meal);
        context.HydrationEntries.Add(hydration);
        context.WeightEntries.Add(weight);
        context.WaistEntries.Add(waist);
        await context.SaveChangesAsync();

        var imageObjectDeletionOutbox = new RecordingImageObjectDeletionOutbox();
        var service = new UserCleanupService(context, imageObjectDeletionOutbox, NullLogger<UserCleanupService>.Instance);

        int removed = await service.CleanupDeletedUsersAsync(DateTime.UtcNow.AddDays(-1), batchSize: 10, reassignUserId: null);

        await using FoodDiaryDbContext verificationContext = CreateVerificationContext(context);

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
        Assert.False(await verificationContext.Meals.AnyAsync());
        Assert.False(await verificationContext.HydrationEntries.AnyAsync());
        Assert.False(await verificationContext.WeightEntries.AnyAsync());
        Assert.False(await verificationContext.WaistEntries.AnyAsync());
        Assert.False(await verificationContext.AdminImpersonationSessions.AnyAsync());
        Assert.False(await verificationContext.ClientTasks.AnyAsync());
        Assert.True(await verificationContext.Users.AnyAsync(user => user.Id == survivingUser.Id));
        Assert.Equal(["users/deleted/image-1.webp"], imageObjectDeletionOutbox.ObjectKeys);
    }

    [RequiresDockerFact]
    public async Task CleanupDeletedUsersAsync_WithReassign_ReassignsContentAssetsAndDeletesUser() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        (User? deletedUser, User? survivorUser) = await SeedReassignScenarioAsync(context).ConfigureAwait(false);

        var imageObjectDeletionOutbox = new RecordingImageObjectDeletionOutbox();
        var service = new UserCleanupService(context, imageObjectDeletionOutbox, NullLogger<UserCleanupService>.Instance);

        int removed = await service.CleanupDeletedUsersAsync(
            DateTime.UtcNow.AddDays(-1),
            batchSize: 10,
            reassignUserId: survivorUser.Id.Value).ConfigureAwait(false);

        FoodDiaryDbContext verificationContext = CreateVerificationContext(context);
        await using (verificationContext.ConfigureAwait(false)) {
            Assert.Equal(1, removed);
            await AssertReassignedContentAsync(verificationContext, deletedUser, survivorUser).ConfigureAwait(false);
            Assert.Equal(
                ["users/deleted/meal.webp", "users/deleted/profile.webp"],
                [.. imageObjectDeletionOutbox.ObjectKeys.Order(StringComparer.Ordinal)]);
        }
    }

    [RequiresDockerFact]
    public async Task CleanupDeletedUsersAsync_WithDeletedReassignTarget_FallsBackToDeletePath() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var deletedUser = User.Create("deleted@example.com", "hash");
        deletedUser.MarkDeleted(DateTime.UtcNow.AddDays(-10));

        var deletedTarget = User.Create("deleted-target@example.com", "hash");
        deletedTarget.MarkDeleted(DateTime.UtcNow.AddDays(-2));

        var imageAsset = ImageAsset.Create(deletedUser.Id, "users/deleted/fallback.webp", "https://cdn.example.com/fallback.webp");
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

        context.Users.AddRange(deletedUser, deletedTarget);
        context.ImageAssets.Add(imageAsset);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var imageObjectDeletionOutbox = new RecordingImageObjectDeletionOutbox();
        var service = new UserCleanupService(context, imageObjectDeletionOutbox, NullLogger<UserCleanupService>.Instance);

        int removed = await service.CleanupDeletedUsersAsync(
            DateTime.UtcNow.AddDays(-1),
            batchSize: 10,
            reassignUserId: deletedTarget.Id.Value);

        await using FoodDiaryDbContext verificationContext = CreateVerificationContext(context);

        Assert.Equal(2, removed);
        Assert.False(await verificationContext.Users.AnyAsync(user => user.Id == deletedUser.Id));
        Assert.False(await verificationContext.Products.AnyAsync());
        Assert.False(await verificationContext.ImageAssets.AnyAsync());
        Assert.Equal(["users/deleted/fallback.webp"], imageObjectDeletionOutbox.ObjectKeys);
    }

    [RequiresDockerFact]
    public async Task CleanupUserAsync_WhenCandidateWasRestored_DoesNotDeleteUser() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("restored-before-cleanup@example.com", "hash");
        user.MarkDeleted(DateTime.UtcNow.AddDays(-10));
        context.Users.Add(user);
        await context.SaveChangesAsync();

        user.Restore();
        await context.SaveChangesAsync();

        var service = new UserCleanupService(
            context,
            new RecordingImageObjectDeletionOutbox(),
            NullLogger<UserCleanupService>.Instance);

        bool removed = await service.CleanupUserAsync(
            user.Id,
            reassignTarget: null,
            DateTime.UtcNow.AddDays(-1),
            CancellationToken.None);

        await using FoodDiaryDbContext verificationContext = CreateVerificationContext(context);
        Assert.False(removed);
        Assert.True(await verificationContext.Users.AnyAsync(candidate => candidate.Id == user.Id));
    }

    private static FoodDiaryDbContext CreateVerificationContext(FoodDiaryDbContext sourceContext) {
        string connectionString = sourceContext.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Source context does not have a connection string.");

        DbContextOptions<FoodDiaryDbContext> options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseNpgsql(new NpgsqlConnectionStringBuilder(connectionString).ConnectionString)
            .Options;

        return new FoodDiaryDbContext(options);
    }

    private static async Task<(User DeletedUser, User SurvivorUser)> SeedReassignScenarioAsync(
        FoodDiaryDbContext context,
        CancellationToken cancellationToken = default) {
        var deletedUser = User.Create("deleted@example.com", "hash");
        var survivorUser = User.Create("survivor@example.com", "hash");
        survivorUser.UpdateProfileMedia(profileImageAssetId: null);

        context.Users.AddRange(deletedUser, survivorUser);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

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
        var recipe = Recipe.Create(deletedUser.Id, "Toast", servings: 1, imageAssetId: recipeAsset.Id);
        recipe.AddStep(1, "Toast bread", imageAssetId: stepAsset.Id);
        var meal = FoodDiary.Domain.Entities.Meals.Meal.Create(
            deletedUser.Id,
            new DateTime(2026, 3, 29, 0, 0, 0, DateTimeKind.Utc),
            imageAssetId: mealAsset.Id);
        var shoppingList = ShoppingList.Create(deletedUser.Id, "Temporary");
        shoppingList.AddItem("Bread", product.Id, 2, MeasurementUnit.Pcs, "Bakery", isChecked: false, 0);

        context.ImageAssets.AddRange(productAsset, recipeAsset, stepAsset, profileAsset, mealAsset);
        context.Products.Add(product);
        context.Recipes.Add(recipe);
        context.Meals.Add(meal);
        context.ShoppingLists.Add(shoppingList);
        context.RecentItems.Add(RecentItem.Create(deletedUser.Id, RecentItemType.Recipe, recipe.Id.Value));
        context.AiUsages.Add(AiUsage.Create(deletedUser.Id, "nutrition", "gpt-4.1-mini", 15, 25, 40));
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        deletedUser.UpdateProfileMedia(profileImageAssetId: profileAsset.Id);
        deletedUser.MarkDeleted(DateTime.UtcNow.AddDays(-10));
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return (deletedUser, survivorUser);
    }

    private static async Task AssertReassignedContentAsync(
        FoodDiaryDbContext verificationContext,
        User deletedUser,
        User survivorUser,
        CancellationToken cancellationToken = default) {
        Assert.False(await verificationContext.Users
            .AnyAsync(user => user.Id == deletedUser.Id, cancellationToken)
            .ConfigureAwait(false));
        Assert.True(await verificationContext.Users
            .AnyAsync(user => user.Id == survivorUser.Id, cancellationToken)
            .ConfigureAwait(false));

        Product reassignedProduct = await verificationContext.Products.SingleAsync(cancellationToken).ConfigureAwait(false);
        Recipe reassignedRecipe = await verificationContext.Recipes.SingleAsync(cancellationToken).ConfigureAwait(false);
        List<ImageAsset> reassignedAssets = await verificationContext.ImageAssets
            .OrderBy(asset => asset.ObjectKey)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        Assert.Equal(survivorUser.Id, reassignedProduct.UserId);
        Assert.Equal(survivorUser.Id, reassignedRecipe.UserId);
        Assert.All(reassignedAssets, asset => Assert.Equal(survivorUser.Id, asset.UserId));
        Assert.Single(await verificationContext.RecipeSteps.ToListAsync(cancellationToken).ConfigureAwait(false));
        Assert.False(await verificationContext.Meals.AnyAsync(cancellationToken).ConfigureAwait(false));
        Assert.False(await verificationContext.ShoppingLists.AnyAsync(cancellationToken).ConfigureAwait(false));
        Assert.False(await verificationContext.ShoppingListItems.AnyAsync(cancellationToken).ConfigureAwait(false));
        Assert.False(await verificationContext.RecentItems.AnyAsync(cancellationToken).ConfigureAwait(false));
        Assert.False(await verificationContext.AiUsages.AnyAsync(cancellationToken).ConfigureAwait(false));
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingImageObjectDeletionOutbox : IImageObjectDeletionOutbox {
        private readonly List<(string ObjectKey, bool IsConfirmed)> _requests = [];

        public IReadOnlyList<string> ObjectKeys => [.. _requests
            .Select(static request => request.ObjectKey)
            .Distinct(StringComparer.Ordinal)];

        public Task EnqueueAsync(string objectKey, bool isConfirmed, CancellationToken cancellationToken = default) {
            _requests.Add((objectKey, isConfirmed));
            return Task.CompletedTask;
        }
    }
}
