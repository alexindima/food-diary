using FoodDiary.Domain.Entities.FavoriteRecipes;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.FavoriteRecipes;
using FoodDiary.Infrastructure.Persistence.Recipes;
using System.Diagnostics;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FoodDiary.Infrastructure.Tests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class RecipeRepositoryIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    private const int PerformanceSeedCount = 1500;
    private static readonly TimeSpan FirstPageLatencyBudget = TimeSpan.FromMilliseconds(250);

    [RequiresDockerFact]
    public async Task GetPagedAsync_EscapesLikePatternAndReturnsExactRecipeMatch() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"recipes-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        UserId userId = user.Id;
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

        (IReadOnlyList<(Recipe Recipe, int UsageCount)>? items, int totalItems) = await repository.GetPagedAsync(
            userId,
            includePublic: false,
            page: 0,
            limit: 0,
            search: "100% Soup");

        (Recipe Recipe, int UsageCount) item = Assert.Single(items);
        Assert.Equal(1, totalItems);
        Assert.Equal(matchingRecipe.Id, item.Recipe.Id);
    }

    [RequiresDockerFact]
    public async Task GetPagedAsync_FirstOwnedPage_StaysWithinLatencyBudget() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"recipes-perf-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        Recipe[] recipes = [.. Enumerable.Range(0, PerformanceSeedCount)
            .Select(index => Recipe.Create(
                user.Id,
                string.Create(CultureInfo.InvariantCulture, $"Perf Recipe {index:D4}"),
                servings: 2,
                description: string.Create(CultureInfo.InvariantCulture, $"Description {index:D4}")))];

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
        (IReadOnlyList<(Recipe Recipe, int UsageCount)>? items, int totalItems) = await repository.GetPagedAsync(
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
            string.Create(CultureInfo.InvariantCulture, $"Expected RecipeRepository.GetPagedAsync first page to stay within {FirstPageLatencyBudget.TotalMilliseconds} ms on seeded PostgreSQL data, but observed {stopwatch.Elapsed.TotalMilliseconds:F1} ms."));
    }

    [RequiresDockerFact]
    public async Task FavoriteRecipeRepository_WithRecipeStepsAndIngredients_DoesNotUseSingleQueryMultipleCollectionInclude() {
        string connectionString = await databaseFixture.CreateIsolatedDatabaseAsync();
        DbContextOptions<FoodDiaryDbContext> options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseNpgsql(connectionString)
            .ConfigureWarnings(warnings => warnings.Throw(RelationalEventId.MultipleCollectionIncludeWarning))
            .Options;

        await using var context = new FoodDiaryDbContext(options);
        await context.Database.MigrateAsync();

        var user = User.Create($"favorite-recipes-{Guid.NewGuid():N}@example.com", "hash");
        var product = Product.Create(
            user.Id,
            "Rice",
            MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 100,
            caloriesPerBase: 130,
            proteinsPerBase: 2.7,
            fatsPerBase: 0.3,
            carbsPerBase: 28,
            fiberPerBase: 0.4,
            alcoholPerBase: 0);
        var recipe = Recipe.Create(user.Id, "Rice bowl", servings: 2);
        recipe.AddStep(1, "Cook rice").AddProductIngredient(product.Id, 100);
        context.Users.Add(user);
        context.Products.Add(product);
        context.Recipes.Add(recipe);
        await context.SaveChangesAsync();

        var repository = new FavoriteRecipeRepository(context);
        FavoriteRecipe favorite = await repository.AddAsync(FavoriteRecipe.Create(user.Id, recipe.Id, "Dinner"));

        FavoriteRecipe? byId = await repository.GetByIdAsync(favorite.Id, user.Id);
        IReadOnlyList<FavoriteRecipe> all = await repository.GetAllAsync(user.Id);

        Assert.NotNull(byId);
        Assert.Equal(favorite.Id, Assert.Single(all).Id);
    }
}
