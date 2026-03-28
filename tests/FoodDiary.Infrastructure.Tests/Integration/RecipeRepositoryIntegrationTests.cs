using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Infrastructure.Persistence.Recipes;
using System.Diagnostics;

namespace FoodDiary.Infrastructure.Tests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
public sealed class RecipeRepositoryIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    private const int PerformanceSeedCount = 1500;
    private static readonly TimeSpan FirstPageLatencyBudget = TimeSpan.FromMilliseconds(250);

    [RequiresDockerFact]
    public async Task GetPagedAsync_EscapesLikePatternAndReturnsExactRecipeMatch() {
        await using var context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"recipes-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var userId = user.Id;
        var matchingRecipe = Recipe.Create(
            userId,
            "100% Soup",
            servings: 2,
            description: "Creamy special");
        var otherRecipe = Recipe.Create(
            userId,
            "1000 Soup",
            servings: 2,
            description: "Creamy regular");
        context.Recipes.AddRange(matchingRecipe, otherRecipe);
        await context.SaveChangesAsync();

        var repository = new RecipeRepository(context);

        var (items, totalItems) = await repository.GetPagedAsync(
            userId,
            includePublic: false,
            page: 0,
            limit: 0,
            search: "100% Soup");

        var item = Assert.Single(items);
        Assert.Equal(1, totalItems);
        Assert.Equal(matchingRecipe.Id, item.Recipe.Id);
    }

    [RequiresDockerFact]
    public async Task GetPagedAsync_FirstOwnedPage_StaysWithinLatencyBudget() {
        await using var context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"recipes-perf-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var recipes = Enumerable.Range(0, PerformanceSeedCount)
            .Select(index => Recipe.Create(
                user.Id,
                $"Perf Recipe {index:D4}",
                servings: 2,
                description: $"Description {index:D4}"))
            .ToArray();

        context.Recipes.AddRange(recipes);
        await context.SaveChangesAsync();

        var repository = new RecipeRepository(context);

        _ = await repository.GetPagedAsync(
            user.Id,
            includePublic: false,
            page: 1,
            limit: 25,
            search: null);

        var stopwatch = Stopwatch.StartNew();
        var (items, totalItems) = await repository.GetPagedAsync(
            user.Id,
            includePublic: false,
            page: 1,
            limit: 25,
            search: null);
        stopwatch.Stop();

        Assert.Equal(PerformanceSeedCount, totalItems);
        Assert.Equal(25, items.Count);
        Assert.True(
            stopwatch.Elapsed <= FirstPageLatencyBudget,
            $"Expected RecipeRepository.GetPagedAsync first page to stay within {FirstPageLatencyBudget.TotalMilliseconds} ms on seeded PostgreSQL data, but observed {stopwatch.Elapsed.TotalMilliseconds:F1} ms.");
    }
}
