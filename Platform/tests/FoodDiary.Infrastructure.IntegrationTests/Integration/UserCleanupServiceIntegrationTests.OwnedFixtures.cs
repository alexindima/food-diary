using FoodDiary.Domain.Primitives;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Infrastructure.Persistence.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

public sealed partial class UserCleanupServiceIntegrationTests {
    [RequiresDockerTheory]
    [InlineData("run")]
    [InlineData("none")]
    [InlineData("too-many")]
    [InlineData("empty-id")]
    [InlineData("duplicate")]
    public async Task CleanupOwnedFixtures_RejectsInvalidIdentitySetsWithoutRemovingTheOwnedAccountOrContent(string invalidField) {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var runId = Guid.NewGuid();
        var owned = User.Create($"webmcp-fixture-{runId:D}-client@example.test", "hash");
        owned.MarkDeleted(DateTime.UtcNow.AddDays(-1));
        var recipe = Recipe.Create(owned.Id, "Retained fixture recipe", servings: 1, visibility: Visibility.Private);
        context.AddRange(owned, recipe);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        await using ServiceProvider provider = CreateServiceProvider(context, new RecordingImageObjectDeletionOutbox(), out UserCleanupService service);
        Guid[] ids = invalidField switch {
            "none" => [],
            "too-many" => [.. Enumerable.Range(0, 21).Select(_ => Guid.NewGuid())],
            "empty-id" => [owned.Id.Value, Guid.Empty],
            "duplicate" => [owned.Id.Value, owned.Id.Value],
            _ => [owned.Id.Value],
        };

        ArgumentException error = await Assert.ThrowsAsync<ArgumentException>(() => service.CleanupOwnedFixtureUsersAsync(
            string.Equals(invalidField, "run", StringComparison.Ordinal) ? Guid.Empty : runId, ids, DateTime.UtcNow, CancellationToken.None));

        Assert.Equal("userIds", error.ParamName);
        await using FoodDiaryDbContext verification = CreateVerificationContext(context);
        Assert.True(await verification.Users.AnyAsync(user => user.Id == owned.Id));
        Assert.True(await verification.Recipes.AnyAsync(item => item.Id == recipe.Id));
        Assert.Null(context.Database.CurrentTransaction);
    }

    [RequiresDockerFact]
    public async Task CleanupOwnedFixtures_PurgesOnlyTheExplicitDeletedSyntheticUser() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var runId = Guid.NewGuid();
        var owned = User.Create($"webmcp-fixture-{runId:D}-client@example.test", "hash");
        var unrelated = User.Create("unrelated-deleted@example.test", "hash");
        DateTime deletedAt = DateTime.UtcNow.AddDays(-1);
        owned.MarkDeleted(deletedAt);
        unrelated.MarkDeleted(deletedAt);
        context.Users.AddRange(owned, unrelated);
        var child = Recipe.Create(owned.Id, "Owned child", servings: 2, visibility: Visibility.Private);
        var parent = Recipe.Create(owned.Id, "Owned parent", servings: 1, visibility: Visibility.Private);
        parent.AddStep(1, "Use half a serving").AddNestedRecipeIngredient(child.Id, RecipeServingQuantity.FromServings(0.5));
        context.Recipes.AddRange(child, parent);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        await using ServiceProvider provider = CreateServiceProvider(context, new RecordingImageObjectDeletionOutbox(), out UserCleanupService service);

        int removed = await service.CleanupOwnedFixtureUsersAsync(runId, [owned.Id.Value], DateTime.UtcNow, CancellationToken.None);

        await using FoodDiaryDbContext verification = CreateVerificationContext(context);
        Assert.Multiple(
            () => Assert.Equal(1, removed),
            () => Assert.False(verification.Users.Any(user => user.Id == owned.Id)),
            () => Assert.True(verification.Users.Any(user => user.Id == unrelated.Id)),
            () => Assert.Empty(verification.Recipes),
            () => Assert.Empty(verification.RecipeIngredients));
    }

    [RequiresDockerFact]
    public async Task CleanupOwnedFixtures_DoesNotRemoveForeignNestedReferences_AndRefusesCleanup() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var runId = Guid.NewGuid();
        var owned = User.Create($"webmcp-fixture-{runId:D}-client@example.test", "hash");
        var foreign = User.Create("foreign-recipe-owner@example.test", "hash");
        owned.MarkDeleted(DateTime.UtcNow.AddDays(-1));
        var child = Recipe.Create(owned.Id, "Public child", servings: 2);
        var ownParent = Recipe.Create(owned.Id, "Own parent", servings: 1);
        var foreignParent = Recipe.Create(foreign.Id, "Foreign parent", servings: 1);
        ownParent.AddStep(1, "Own edge").AddNestedRecipeIngredient(child.Id, RecipeServingQuantity.FromServings(1));
        foreignParent.AddStep(1, "Foreign edge").AddNestedRecipeIngredient(child.Id, RecipeServingQuantity.FromServings(1));
        context.Users.AddRange(owned, foreign);
        context.Recipes.AddRange(child, ownParent, foreignParent);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        await using ServiceProvider provider = CreateServiceProvider(context, new RecordingImageObjectDeletionOutbox(), out UserCleanupService service);

        int removed = await service.CleanupOwnedFixtureUsersAsync(runId, [owned.Id.Value], DateTime.UtcNow, CancellationToken.None);
        await using FoodDiaryDbContext verification = CreateVerificationContext(context);
        Assert.Multiple(
            () => Assert.Equal(0, removed),
            () => Assert.True(verification.Users.Any(user => user.Id == owned.Id)),
            () => Assert.Equal(3, verification.Recipes.Count()),
            () => Assert.Equal(2, verification.RecipeIngredients.Count()));
    }

    [RequiresDockerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CleanupOwnedFixtures_ValidatesTheCompleteSetBeforeAnyPurge(bool activeSynthetic) {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var runId = Guid.NewGuid();
        var owned = User.Create($"webmcp-fixture-{runId:D}-client@example.test", "hash");
        var rejected = User.Create(activeSynthetic ? $"webmcp-fixture-{runId:D}-dietologist@example.test" : "foreign@example.test", "hash");
        DateTime deletedAt = DateTime.UtcNow.AddDays(-1);
        owned.MarkDeleted(deletedAt);
        if (!activeSynthetic) {
            rejected.MarkDeleted(deletedAt);
        }
        context.Users.AddRange(owned, rejected);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        await using ServiceProvider provider = CreateServiceProvider(context, new RecordingImageObjectDeletionOutbox(), out UserCleanupService service);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CleanupOwnedFixtureUsersAsync(
            runId, [owned.Id.Value, rejected.Id.Value], DateTime.UtcNow, CancellationToken.None));

        await using FoodDiaryDbContext verification = CreateVerificationContext(context);
        Assert.Equal(2, await verification.Users.CountAsync());
    }
}
