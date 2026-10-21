using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Recipes.Contracts.Models;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.ReadModel.Composition.Recipes;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class RecipeTotalTimeIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task GetPagedAsync_WithMaximumTimeFilter_ExcludesOnlyTotalsAboveTheBoundAsync() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"recipe-time-filter-{Guid.NewGuid():N}@example.test", "hash");
        Recipe[] recipes = [
            Recipe.Create(user.Id, "Above Int32 total", 1, prepTime: int.MaxValue, cookTime: 1),
            Recipe.Create(user.Id, "At Int32 total", 1, prepTime: int.MaxValue - 1, cookTime: 1),
            Recipe.Create(user.Id, "Short known time", 1, cookTime: 5),
            Recipe.Create(user.Id, "Unknown time", 1),
        ];
        context.Users.Add(user);
        context.Recipes.AddRange(recipes);
        await context.SaveChangesAsync();
        var service = new RecipeOverviewReadService(context);

        (IReadOnlyList<RecipeOverviewReadItem> items, int total) = await service.GetPagedAsync(
            user.Id, includePublic: false, page: 1, limit: 10,
            filters: new RecipeQueryFilters(Search: null, MaxTotalTime: int.MaxValue, SortBy: "fastest"));

        Assert.Equal(2, total);
        Assert.Equal([recipes[2].Id, recipes[1].Id], items.Select(item => item.Id));
        RecipeOverviewReadItem boundary = items[1];
        Assert.Multiple(
            () => Assert.Equal(int.MaxValue - 1, boundary.PrepTime),
            () => Assert.Equal(1, boundary.CookTime));
    }

    [RequiresDockerTheory]
    [InlineData("fastest", 0, 1, 2, 3)]
    [InlineData("slowest", 2, 1, 0, 3)]
    public async Task GetPagedAsync_WithLargeTimeTotals_OrdersBeforePagingAndRetainsUnknownLastAsync(
        string sort, int first, int second, int third, int fourth) {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"recipe-time-order-{Guid.NewGuid():N}@example.test", "hash");
        Recipe[] recipes = [
            Recipe.Create(user.Id, "Short known time", 1, cookTime: 5),
            Recipe.Create(user.Id, "At Int32 total", 1, prepTime: int.MaxValue - 1, cookTime: 1),
            Recipe.Create(user.Id, "Above Int32 total", 1, prepTime: int.MaxValue, cookTime: 1),
            Recipe.Create(user.Id, "Unknown time", 1),
        ];
        context.Users.Add(user);
        context.Recipes.AddRange(recipes);
        await context.SaveChangesAsync();
        var service = new RecipeOverviewReadService(context);
        int[] expected = [first, second, third, fourth];

        for (int index = 0; index < expected.Length; index++) {
            (IReadOnlyList<RecipeOverviewReadItem> items, int total) = await service.GetPagedAsync(
                user.Id, includePublic: false, page: index + 1, limit: 1,
                filters: new RecipeQueryFilters(Search: null, SortBy: sort));

            Assert.Equal(recipes.Length, total);
            RecipeOverviewReadItem item = Assert.Single(items);
            Recipe recipe = recipes[expected[index]];
            Assert.Multiple(
                () => Assert.Equal(recipe.Id, item.Id),
                () => Assert.Equal(recipe.PrepTime, item.PrepTime),
                () => Assert.Equal(recipe.CookTime, item.CookTime));
        }
    }
}
