using FoodDiary.Domain.Entities.FavoriteRecipes;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Recipes.Models;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.FavoriteRecipes;
using FoodDiary.Infrastructure.Persistence.Recipes;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FoodDiary.Infrastructure.Tests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class RecipeRepositoryIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    private const int PerformanceSeedCount = 1500;
    private static readonly TimeSpan FirstPageLatencyBudget = TimeSpan.FromMilliseconds(250);

    [Fact]
    public void CalculateAutoNutrition_WithNestedRecipeIngredient_UsesNestedRecipeNutritionPerServing() {
        var ingredient = new RecipeOverviewIngredientReadItem(
            Guid.NewGuid(),
            Amount: 2,
            ProductId: null,
            ProductName: null,
            ProductBaseUnit: null,
            ProductBaseAmount: null,
            ProductCaloriesPerBase: null,
            ProductProteinsPerBase: null,
            ProductFatsPerBase: null,
            ProductCarbsPerBase: null,
            ProductFiberPerBase: null,
            ProductAlcoholPerBase: null,
            NestedRecipeId: Guid.NewGuid(),
            NestedRecipeName: "Sauce",
            NestedRecipeServings: 4,
            NestedRecipeTotalCalories: 320,
            NestedRecipeTotalProteins: 12,
            NestedRecipeTotalFats: 20,
            NestedRecipeTotalCarbs: 24,
            NestedRecipeTotalFiber: 6,
            NestedRecipeTotalAlcohol: 2);
        var step = new RecipeOverviewStepReadItem(
            Guid.NewGuid(),
            StepNumber: 1,
            Title: null,
            "Mix nested recipe",
            ImageUrl: null,
            ImageAssetId: null,
            [ingredient]);

        IReadOnlyList<RecipeOverviewStepReadItem> steps = [step];
        object summary = InvokeRecipeOverviewStatic<object>("CalculateAutoNutrition", steps);

        Assert.Multiple(
            () => Assert.Equal(160d, GetPrivateProperty<double?>(summary, "TotalCalories")),
            () => Assert.Equal(6d, GetPrivateProperty<double?>(summary, "TotalProteins")),
            () => Assert.Equal(10d, GetPrivateProperty<double?>(summary, "TotalFats")),
            () => Assert.Equal(12d, GetPrivateProperty<double?>(summary, "TotalCarbs")),
            () => Assert.Equal(3d, GetPrivateProperty<double?>(summary, "TotalFiber")),
            () => Assert.Equal(1d, GetPrivateProperty<double?>(summary, "TotalAlcohol")));
    }

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

        var readService = new RecipeOverviewReadService(context);

        (IReadOnlyList<RecipeOverviewReadItem>? items, int totalItems) = await readService.GetPagedAsync(
            userId,
            includePublic: false,
            page: 0,
            limit: 0,
            filters: new RecipeQueryFilters("100% Soup"));

        RecipeOverviewReadItem item = Assert.Single(items);
        Assert.Equal(1, totalItems);
        Assert.Equal(matchingRecipe.Id, item.Id);
    }

    [RequiresDockerFact]
    public async Task GetPagedAsync_AppliesStructuredFilters() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"recipes-filters-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        (Recipe quickSaladWithImage, Recipe longSaladNoImage, Recipe quickSoupNoImage) =
            await SeedRecipeFiltersAsync(context, user.Id);
        var readService = new RecipeOverviewReadService(context);

        IReadOnlyList<RecipeId> saladIds = await GetRecipeIdsAsync(readService, user.Id, new RecipeQueryFilters(
            Search: null,
            Category: "salad"));
        IReadOnlyList<RecipeId> quickIds = await GetRecipeIdsAsync(readService, user.Id, new RecipeQueryFilters(
            Search: null,
            MaxTotalTime: 25));
        IReadOnlyList<RecipeId> mediumCalorieIds = await GetRecipeIdsAsync(readService, user.Id, new RecipeQueryFilters(
            Search: null,
            CaloriesFrom: 200,
            CaloriesTo: 300));
        IReadOnlyList<RecipeId> withImageIds = await GetRecipeIdsAsync(readService, user.Id, new RecipeQueryFilters(
            Search: null,
            HasImage: true));
        IReadOnlyList<RecipeId> withoutImageIds = await GetRecipeIdsAsync(readService, user.Id, new RecipeQueryFilters(
            Search: null,
            HasImage: false));

        AssertIds([quickSaladWithImage.Id, longSaladNoImage.Id], saladIds);
        AssertIds([quickSaladWithImage.Id, quickSoupNoImage.Id], quickIds);
        Assert.Equal([quickSoupNoImage.Id], mediumCalorieIds);
        Assert.Equal([quickSaladWithImage.Id], withImageIds);
        AssertIds([longSaladNoImage.Id, quickSoupNoImage.Id], withoutImageIds);
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

        var readService = new RecipeOverviewReadService(context);

        _ = await readService.GetPagedAsync(
            user.Id,
            includePublic: false,
            page: 1,
            limit: 25,
            filters: new RecipeQueryFilters(Search: null));

        var stopwatch = Stopwatch.StartNew();
        (IReadOnlyList<RecipeOverviewReadItem>? items, int totalItems) = await readService.GetPagedAsync(
            user.Id,
            includePublic: false,
            page: 1,
            limit: 25,
            filters: new RecipeQueryFilters(Search: null));
        stopwatch.Stop();

        Assert.Equal(PerformanceSeedCount, totalItems);
        Assert.Equal(25, items.Count);
        Assert.True(
            stopwatch.Elapsed <= FirstPageLatencyBudget,
            string.Create(CultureInfo.InvariantCulture, $"Expected RecipeOverviewReadService.GetPagedAsync first page to stay within {FirstPageLatencyBudget.TotalMilliseconds} ms on seeded PostgreSQL data, but observed {stopwatch.Elapsed.TotalMilliseconds:F1} ms."));
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
        await context.SaveChangesAsync();

        FavoriteRecipe? byId = await repository.GetByIdAsync(favorite.Id, user.Id);
        IReadOnlyList<FavoriteRecipe> all = await repository.GetAllAsync(user.Id);

        Assert.NotNull(byId);
        Assert.Equal(favorite.Id, Assert.Single(all).Id);
    }

    private static async Task<IReadOnlyList<RecipeId>> GetRecipeIdsAsync(
        RecipeOverviewReadService readService,
        UserId userId,
        RecipeQueryFilters filters) {
        (IReadOnlyList<RecipeOverviewReadItem> items, int _) = await readService.GetPagedAsync(
            userId,
            includePublic: false,
            page: 1,
            limit: 50,
            filters: filters).ConfigureAwait(false);

        return [.. items.Select(item => item.Id)];
    }

    private static async Task<(Recipe QuickSaladWithImage, Recipe LongSaladNoImage, Recipe QuickSoupNoImage)> SeedRecipeFiltersAsync(
        FoodDiaryDbContext context,
        UserId userId) {
        var quickSaladWithImage = Recipe.Create(
            userId,
            "Quick salad",
            servings: 2,
            category: "Salad",
            imageUrl: "https://cdn.example.com/salad.webp",
            prepTime: 10,
            cookTime: 0);
        quickSaladWithImage.ApplyComputedNutrition(180, 6, 9, 20, 6, 0);

        var longSaladNoImage = Recipe.Create(userId, "Roasted salad", servings: 2, category: "Salad", prepTime: 20, cookTime: 45);
        longSaladNoImage.ApplyComputedNutrition(520, 18, 31, 42, 8, 0);

        var quickSoupNoImage = Recipe.Create(userId, "Tomato soup", servings: 4, category: "Soup", prepTime: 5, cookTime: 20);
        quickSoupNoImage.ApplyComputedNutrition(240, 7, 6, 40, 7, 0);

        context.Recipes.AddRange(quickSaladWithImage, longSaladNoImage, quickSoupNoImage);
        await context.SaveChangesAsync().ConfigureAwait(false);
        return (quickSaladWithImage, longSaladNoImage, quickSoupNoImage);
    }

    private static void AssertIds(IReadOnlyCollection<RecipeId> expected, IReadOnlyCollection<RecipeId> actual) =>
        Assert.Equal(
            [.. expected.Select(id => id.Value).Order()],
            [.. actual.Select(id => id.Value).Order()]);

    private static T InvokeRecipeOverviewStatic<T>(string methodName, params object?[] arguments) {
        MethodInfo method = typeof(RecipeOverviewReadService)
            .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .Single(candidate =>
                string.Equals(candidate.Name, methodName, StringComparison.Ordinal) &&
                candidate.GetParameters().Length == arguments.Length);
        return (T)method.Invoke(null, arguments)!;
    }

    private static T GetPrivateProperty<T>(object instance, string propertyName) {
        PropertyInfo property = instance.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
        return (T)property.GetValue(instance)!;
    }
}
