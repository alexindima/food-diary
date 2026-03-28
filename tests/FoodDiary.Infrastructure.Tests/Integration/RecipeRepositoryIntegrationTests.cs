using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Infrastructure.Persistence.Recipes;

namespace FoodDiary.Infrastructure.Tests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
public sealed class RecipeRepositoryIntegrationTests(PostgresDatabaseFixture databaseFixture) {
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
}
