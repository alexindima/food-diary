using FoodDiary.Application.Recipes.Queries.GetRecentRecipes;
using FoodDiary.Application.Recipes.Queries.GetRecipesWithRecent;

namespace FoodDiary.Application.Tests.Recipes;

public class RecipesFeatureTests {
    [Fact]
    public async Task GetRecentRecipesQueryValidator_WithEmptyUserId_Fails() {
        var validator = new GetRecentRecipesQueryValidator();
        var query = new GetRecentRecipesQuery(Guid.Empty, 10, true);

        var result = await validator.ValidateAsync(query);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task GetRecipesWithRecentQueryValidator_WithValidUserId_Passes() {
        var validator = new GetRecipesWithRecentQueryValidator();
        var query = new GetRecipesWithRecentQuery(Guid.NewGuid(), 1, 10, null, true, 10);

        var result = await validator.ValidateAsync(query);

        Assert.True(result.IsValid);
    }
}
