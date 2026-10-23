using FoodDiary.Domain.Primitives;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Favorites.Domain.Entities.FavoriteProducts;
using FoodDiary.Modules.Favorites.Domain.Entities.FavoriteRecipes;
using FoodDiary.Modules.MealPlanning.Domain.Entities.Shopping;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Products.Domain.Entities;
using FoodDiary.Modules.RecipeCommunity.Domain.Entities.Recipes;
using FoodDiary.Modules.RecipeCommunity.Domain.Entities.Social;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Users.Application.Abstractions.Models;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Infrastructure.Persistence;
using FoodDiary.Modules.Users.Infrastructure.Persistence.Users;
using FoodDiary.Persistence.Abstractions;
using FoodDiary.Persistence.Runtime.Persistence.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

public sealed partial class UserCleanupServiceIntegrationTests {
    [RequiresDockerTheory]
    [InlineData("recipe-favorite")]
    [InlineData("comment")]
    [InlineData("like")]
    [InlineData("product-favorite")]
    [InlineData("shopping")]
    public async Task CleanupDeletedUsersAsync_ReferenceCommittedAfterNegativePreflight_DefersUntilNextFullPass(string reference) {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        RetentionRaceScenario scenario = await SeedRetentionRaceAsync(context);
        await using ServiceProvider provider = CreateServiceProvider(context, new RecordingImageObjectDeletionOutbox(), out _);
        var gate = new RetentionPreflightGate();
        var service = new UserCleanupService(provider.GetRequiredService<UsersDbContext>(),
            provider.GetServices<IUserDataPurgeParticipant>(), NullLogger<UserCleanupService>.Instance,
            provider.GetRequiredService<IModuleTransactionCoordinator>(), new EfModuleScopeGuard(context),
            provider.GetServices<IUserContentRetentionParticipant>().Append(gate));

        Task<UserCleanupBatch> cleanup = service.CleanupDeletedUsersAsync(DateTime.UtcNow, 10, reassignUserId: null);
        try {
            await gate.Reached.Task.WaitAsync(TimeSpan.FromSeconds(30));
            await using FoodDiaryDbContext consumer = CreateVerificationContext(context);
            AddRetentionRaceReference(consumer, scenario, reference);
            await consumer.SaveChangesAsync();
        } finally {
            gate.Release.TrySetResult();
            await cleanup.WaitAsync(TimeSpan.FromSeconds(30));
        }
        UserCleanupBatch first = await cleanup.WaitAsync(TimeSpan.FromSeconds(30));

        Assert.Equal(0, first.RemovedCount);
        await using FoodDiaryDbContext observer = CreateVerificationContext(context);
        Assert.True(await observer.Users.AnyAsync(user => user.Id == scenario.Deleted.Id));
        await AssertRetentionRaceReferenceAsync(observer, scenario, reference);
        Assert.False(await observer.Users.AnyAsync(user => user.Id == SystemContentOwner.Id));

        // A later full pass starts without the last-examined cursor and provisions outside the item.
        UserCleanupBatch next = await service.CleanupDeletedUsersAsync(DateTime.UtcNow, 10, reassignUserId: null);
        Assert.Equal(1, next.RemovedCount);
        await AssertRetentionRaceReferenceAsync(observer, scenario, reference);
        if (reference is "product-favorite" or "shopping") {
            Assert.Equal(SystemContentOwner.Id, (await observer.Products.SingleAsync(product => product.Id == scenario.Product.Id)).UserId);
        } else {
            Assert.Equal(SystemContentOwner.Id, (await observer.Recipes.SingleAsync(recipe => recipe.Id == scenario.Recipe.Id)).UserId);
        }
    }

    [RequiresDockerTheory]
    [InlineData("recipe-favorite")]
    [InlineData("comment")]
    [InlineData("like")]
    [InlineData("product-favorite")]
    [InlineData("shopping")]
    public async Task CleanupDeletedUsersAsync_ReferenceInsertAfterSelection_BlocksThenFailsInsteadOfLosingASuccessfulWrite(string reference) {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        RetentionRaceScenario scenario = await SeedRetentionRaceAsync(context);
        var gate = new RetentionPurgeGate();
        await using ServiceProvider provider = CreateServiceProvider(context, new RecordingImageObjectDeletionOutbox(),
            out UserCleanupService service, gate);
        await using FoodDiaryDbContext consumer = CreateVerificationContext(context);
        Task<UserCleanupBatch> cleanup = service.CleanupDeletedUsersAsync(DateTime.UtcNow, 10, reassignUserId: null);
        Task<int>? insert = null;
        try {
            await gate.Reached.Task.WaitAsync(TimeSpan.FromSeconds(30));
            await consumer.Database.OpenConnectionAsync();
            int consumerBackendId = ((NpgsqlConnection)consumer.Database.GetDbConnection()).ProcessID;
            AddRetentionRaceReference(consumer, scenario, reference);
            insert = consumer.SaveChangesAsync();
            await using FoodDiaryDbContext observer = CreateVerificationContext(context);
            bool blocked = await WaitForRetentionConsumerLockAsync(observer, consumerBackendId, insert);
            gate.Release.TrySetResult();
            UserCleanupBatch result = await cleanup.WaitAsync(TimeSpan.FromSeconds(30));
            Exception? failure = await Record.ExceptionAsync(() => insert.WaitAsync(TimeSpan.FromSeconds(30)));

            Assert.True(blocked, "The consumer must wait on the content row locked before authoritative retention selection.");
            Assert.Equal(1, result.RemovedCount);
            DbUpdateException update = Assert.IsType<DbUpdateException>(failure);
            Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, Assert.IsType<PostgresException>(update.InnerException).SqlState, StringComparer.Ordinal);
        } finally {
            gate.Release.TrySetResult();
            await cleanup.WaitAsync(TimeSpan.FromSeconds(30));
            if (insert is not null) {
                _ = await Record.ExceptionAsync(() => insert.WaitAsync(TimeSpan.FromSeconds(30)));
            }
        }
    }

    [RequiresDockerTheory]
    [InlineData("recipe-favorite")]
    [InlineData("comment")]
    [InlineData("like")]
    [InlineData("product-favorite")]
    [InlineData("shopping")]
    public async Task CleanupOwnedFixtures_ReferencedContentIsRefusedWithoutTransferOrForeignLinkLoss(string reference) {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var runId = Guid.NewGuid();
        RetentionRaceScenario scenario = await SeedRetentionRaceAsync(context, runId);
        AddRetentionRaceReference(context, scenario, reference);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        await using ServiceProvider provider = CreateServiceProvider(context, new RecordingImageObjectDeletionOutbox(), out UserCleanupService service);

        int removed = await service.CleanupOwnedFixtureUsersAsync(runId, [scenario.Deleted.Id.Value], DateTime.UtcNow, CancellationToken.None);

        Assert.Equal(0, removed);
        await using FoodDiaryDbContext observer = CreateVerificationContext(context);
        Assert.True(await observer.Users.AnyAsync(user => user.Id == scenario.Deleted.Id));
        Assert.False(await observer.Users.AnyAsync(user => user.Id == SystemContentOwner.Id));
        Assert.Equal(scenario.Deleted.Id, (await observer.Recipes.SingleAsync(recipe => recipe.Id == scenario.Recipe.Id)).UserId);
        Assert.Equal(scenario.Deleted.Id, (await observer.Products.SingleAsync(product => product.Id == scenario.Product.Id)).UserId);
        await AssertRetentionRaceReferenceAsync(observer, scenario, reference);
    }

    private static async Task<RetentionRaceScenario> SeedRetentionRaceAsync(FoodDiaryDbContext context, Guid? fixtureRunId = null) {
        string email = fixtureRunId is { } runId ? $"webmcp-fixture-{runId:D}-client@example.test" : "retention-race-owner@example.test";
        var deleted = User.Create(email, "hash");
        var survivor = User.Create("retention-race-consumer@example.test", "hash");
        deleted.MarkDeleted(DateTime.UtcNow.AddDays(-10));
        var recipe = Recipe.Create(deleted.Id, "Racing public recipe", servings: 1, visibility: Visibility.Public);
        var product = Product.Create(deleted.Id, "Racing public product", MeasurementUnit.G, 100,
            defaultPortionAmount: null, 100, 1, 1, 1, 0, 0, visibility: Visibility.Public);
        context.AddRange(deleted, survivor, recipe, product);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return new RetentionRaceScenario(deleted, survivor, recipe, product);
    }

    private static void AddRetentionRaceReference(FoodDiaryDbContext context, RetentionRaceScenario scenario, string reference) {
        switch (reference) {
            case "recipe-favorite":
                context.FavoriteRecipes.Add(FavoriteRecipe.Create(scenario.Survivor.Id, scenario.Recipe.Id));
                break;
            case "comment":
                context.RecipeComments.Add(RecipeComment.Create(scenario.Survivor.Id, scenario.Recipe.Id, "Concurrent foreign comment"));
                break;
            case "like":
                context.RecipeLikes.Add(RecipeLike.Create(scenario.Survivor.Id, scenario.Recipe.Id));
                break;
            case "product-favorite":
                context.FavoriteProducts.Add(FavoriteProduct.Create(scenario.Survivor.Id, scenario.Product.Id));
                break;
            case "shopping":
                var shopping = ShoppingList.Create(scenario.Survivor.Id, "Concurrent foreign shopping");
                shopping.AddItem("Concurrent product", scenario.Product.Id, 1, MeasurementUnit.G, category: null, isChecked: false, 0);
                context.ShoppingLists.Add(shopping);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(reference));
        }
    }

    private static async Task AssertRetentionRaceReferenceAsync(FoodDiaryDbContext context, RetentionRaceScenario scenario, string reference) {
        bool exists = reference switch {
            "recipe-favorite" => await context.FavoriteRecipes.AnyAsync(item => item.RecipeId == scenario.Recipe.Id && item.UserId == scenario.Survivor.Id),
            "comment" => await context.RecipeComments.AnyAsync(item => item.RecipeId == scenario.Recipe.Id && item.UserId == scenario.Survivor.Id),
            "like" => await context.RecipeLikes.AnyAsync(item => item.RecipeId == scenario.Recipe.Id && item.UserId == scenario.Survivor.Id),
            "product-favorite" => await context.FavoriteProducts.AnyAsync(item => item.ProductId == scenario.Product.Id && item.UserId == scenario.Survivor.Id),
            "shopping" => await context.ShoppingLists.Where(list => list.UserId == scenario.Survivor.Id).SelectMany(list => list.Items)
                .AnyAsync(item => item.ProductId == scenario.Product.Id),
            _ => throw new ArgumentOutOfRangeException(nameof(reference)),
        };
        Assert.True(exists, "A surviving consumer's committed reference must remain intact.");
    }

    private static async Task<bool> WaitForRetentionConsumerLockAsync(FoodDiaryDbContext observer, int backendId, Task insert) {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        while (!insert.IsCompleted) {
            int blockers = await observer.Database.SqlQuery<int>($"SELECT cardinality(pg_blocking_pids({backendId})) AS \"Value\"")
                .SingleAsync(timeout.Token);
            if (blockers > 0) { return true; }
            timeout.Token.ThrowIfCancellationRequested();
        }
        return false;
    }

    [ExcludeFromCodeCoverage]
    private sealed record RetentionRaceScenario(User Deleted, User Survivor, Recipe Recipe, Product Product);

    [ExcludeFromCodeCoverage]
    private sealed class RetentionPreflightGate : IUserContentRetentionParticipant {
        private int _checks;
        public int Order => int.MaxValue;
        public TaskCompletionSource Reached { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<bool> HasReferencedContentAsync(UserId sourceUserId, CancellationToken cancellationToken) {
            if (Interlocked.Increment(ref _checks) == 1) {
                Reached.TrySetResult();
                await Release.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
            }
            return false;
        }

        public Task LockOwnedContentAsync(UserId sourceUserId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task RetainReferencedContentAsync(UserId sourceUserId, UserId targetUserId, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class RetentionPurgeGate : IUserDataPurgeParticipant {
        public int Order => 1;
        public TaskCompletionSource Reached { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task PurgeAsync(UserId userId, UserId? reassignTarget, CancellationToken cancellationToken) {
            Reached.TrySetResult();
            await Release.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
        }
    }
}
