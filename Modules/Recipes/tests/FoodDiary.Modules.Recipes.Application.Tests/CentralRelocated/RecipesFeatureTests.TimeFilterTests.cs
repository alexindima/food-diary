using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Recipes.Application.Models;
using FoodDiary.Modules.Recipes.Application.Queries.GetRecipesOverview;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.RecentItems.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Results;

namespace FoodDiary.Modules.Recipes.Application.Tests.CentralRelocated;

public partial class RecipesFeatureTests {
    [Theory]
    [InlineData(null, null, 15, false)]
    [InlineData(0, null, 15, true)]
    [InlineData(12, null, 15, true)]
    [InlineData(null, 15, 15, true)]
    [InlineData(12, 15, 15, false)]
    [InlineData(0, 120, 15, false)]
    [InlineData(null, null, null, true)]
    public async Task GetRecipesOverviewQueryHandler_WithTimeFilter_FiltersRecentUnknownAndKnownDurations(
        int? prepTime, int? cookTime, int? maxTotalTime, bool included) {
        var user = User.Create("recent-time-filter@example.test", "hash");
        var recipe = Recipe.Create(user.Id, "Recent time filter", servings: 1, prepTime: prepTime, cookTime: cookTime,
            visibility: Visibility.Private);
        var readService = new OverviewRecipeReadService(
            recipesByIdWithUsage: new Dictionary<RecipeId, (Recipe Recipe, int UsageCount)> {
                [recipe.Id] = (recipe, 1),
            });
        GetRecipesOverviewQueryHandler handler = CreateRecipesOverviewHandler(readService,
            new StubRecentItemRepository([new RecentRecipeUsage(recipe.Id, 1, DateTime.UtcNow)]),
            new StubFavoriteRecipeRepository([]), new StubUserRepository(user));

        Result<RecipeOverviewModel> result = await handler.Handle(
            new GetRecipesOverviewQuery(user.Id.Value, 1, 10, Search: null, IncludePublic: false,
                MaxTotalTime: maxTotalTime), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(included ? [recipe.Id.Value] : Array.Empty<Guid>(),
            result.Value.RecentItems.Select(item => item.Id));
    }
}
