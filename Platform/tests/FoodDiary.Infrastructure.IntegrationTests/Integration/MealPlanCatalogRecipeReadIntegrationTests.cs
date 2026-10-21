using System.Globalization;
using FoodDiary.Domain.Primitives;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Models;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.ReadModel.Composition.MealPlanning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class MealPlanCatalogRecipeReadIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task Search_UsesPublicVisibilityLiteralTrimmedSearchStableOrderingAndMinimumLimitWithoutTracking() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("catalog-reader@example.test", "hash");
        var alpha = Recipe.Create(user.Id, "Alpha", 2);
        var otherAlpha = Recipe.Create(user.Id, "Alpha", 3);
        var beta = Recipe.Create(user.Id, "Beta", 4);
        var literal = Recipe.Create(user.Id, "Literal %_ value", 5);
        var hidden = Recipe.Create(user.Id, "Alpha private", 6, visibility: Visibility.Private);
        context.AddRange(user, alpha, otherAlpha, beta, literal, hidden);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var reader = new MealPlanCatalogRecipeReader(context);
        using var cancellation = new CancellationTokenSource();
        Recipe[] orderedAlpha = [.. new[] { alpha, otherAlpha }.OrderBy(recipe => recipe.Id.Value)];
        CatalogRecipeReadModel[] expectedAlpha = [.. orderedAlpha.Select(recipe => new CatalogRecipeReadModel(recipe.Id.Value, "Alpha", recipe.Servings))];

        IReadOnlyList<CatalogRecipeReadModel> found = await reader.SearchAsync("  Alpha  ", 10, cancellation.Token);

        Assert.Equal(expectedAlpha, found);
        Assert.Equal(expectedAlpha[0], Assert.Single(await reader.SearchAsync("  ", 0, cancellation.Token)));
        Assert.Equal(expectedAlpha[0], Assert.Single(await reader.SearchAsync(search: null, -10, cancellation.Token)));
        Assert.Equal(new CatalogRecipeReadModel(literal.Id.Value, "Literal %_ value", 5),
            Assert.Single(await reader.SearchAsync("%_", 10, cancellation.Token)));
        Assert.Empty(await reader.SearchAsync("missing", 10, cancellation.Token));
        Assert.Empty(context.ChangeTracker.Entries());
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reader.SearchAsync("Alpha", 10, cancellation.Token));
    }

    [RequiresDockerFact]
    public async Task Search_ClampsTheMaximumLimitToOneHundredInSql() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("catalog-cap@example.test", "hash");
        Recipe[] recipes = [.. Enumerable.Range(0, 101).Select(index => Recipe.Create(user.Id, "Cap " + index.ToString("D3", CultureInfo.InvariantCulture), 1))];
        context.Users.Add(user);
        context.Recipes.AddRange(recipes);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        IReadOnlyList<CatalogRecipeReadModel> found = await new MealPlanCatalogRecipeReader(context).SearchAsync("Cap", int.MaxValue, CancellationToken.None);

        Assert.Equal(100, found.Count);
        Assert.Equal(Enumerable.Range(0, 100).Select(index => "Cap " + index.ToString("D3", CultureInfo.InvariantCulture)),
            found.Select(recipe => recipe.Name), StringComparer.Ordinal);
        Assert.DoesNotContain(found, recipe => recipe.Id == recipes[100].Id.Value);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [RequiresDockerFact]
    public async Task PublicIdentityBatch_FiltersOnlyRequestedPublicIdsAndReadsTheCallerTransactionWithoutTracking() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("catalog-batch@example.test", "hash");
        var published = Recipe.Create(user.Id, "Requested public", 1);
        var otherPublic = Recipe.Create(user.Id, "Other public", 1);
        var hidden = Recipe.Create(user.Id, "Requested private", 1, visibility: Visibility.Private);
        context.AddRange(user, published, otherPublic, hidden);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var reader = new MealPlanCatalogRecipeReader(context);
        Guid[] ids = [published.Id.Value, published.Id.Value, hidden.Id.Value, Guid.NewGuid()];

        IReadOnlySet<Guid> found = await reader.GetPublicIdsAsync(ids, CancellationToken.None);

        Assert.Equal(published.Id.Value, Assert.Single(found));
        Assert.Empty(context.ChangeTracker.Entries());
        await using (IDbContextTransaction transaction = await context.Database.BeginTransactionAsync()) {
            Recipe changing = await context.Recipes.SingleAsync(recipe => recipe.Id == hidden.Id);
            changing.ChangeVisibility(Visibility.Public);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            IReadOnlySet<Guid> uncommitted = await reader.GetPublicIdsAsync(ids, CancellationToken.None);

            Assert.True(uncommitted.SetEquals([published.Id.Value, hidden.Id.Value]));
            Assert.Empty(context.ChangeTracker.Entries());
            await transaction.RollbackAsync();
        }
        Assert.Equal(published.Id.Value, Assert.Single(await reader.GetPublicIdsAsync(ids, CancellationToken.None)));
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        Assert.Empty(await reader.GetPublicIdsAsync([], cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reader.GetPublicIdsAsync(ids, cancellation.Token));
        Assert.Empty(context.ChangeTracker.Entries());
    }
}
