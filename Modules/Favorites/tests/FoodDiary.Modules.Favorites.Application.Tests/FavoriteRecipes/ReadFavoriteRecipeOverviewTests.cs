using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteRecipes.Common;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteRecipes.Models;
using FoodDiary.Modules.Favorites.Application.FavoriteRecipes.Queries.ReadFavoriteRecipeOverview;
using FoodDiary.Modules.Favorites.Contracts.FavoriteRecipes.Queries.ReadFavoriteRecipeOverview;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Favorites.Application.Tests.FavoriteRecipes;

[ExcludeFromCodeCoverage]
public sealed class ReadFavoriteRecipeOverviewTests {
    [Theory]
    [InlineData(0, 0)]
    [InlineData(-1, 0)]
    [InlineData(100, 50)]
    public async Task ReadsCountAndOnlyRequestedRecipeStatesAsync(int requestedLimit, int expectedLimit) {
        var owner = UserId.New();
        RecipeId[] ids = [RecipeId.New()];
        using var cancellation = new CancellationTokenSource();
        IFavoriteRecipeQuery queries = Substitute.For<IFavoriteRecipeQuery>();
        FavoriteRecipeReadModel[] preview = [];
        var item = new FavoriteRecipeReadModel(Guid.NewGuid(), ids[0].Value, Name: null, DateTime.UtcNow,
            "Rice", ImageUrl: null, 100, 2, PrepTime: null, CookTime: null, 1);
        queries.GetPageReadModelsAsync(owner, 1, expectedLimit, search: null, cancellation.Token).Returns((preview, 1000));
        queries.GetByRecipeIdsReadModelsAsync(owner, ids, cancellation.Token).Returns(new[] { item });
        var handler = new ReadFavoriteRecipeOverviewQueryHandler(queries);

        FavoriteRecipeOverviewModel result = await handler.Handle(new ReadFavoriteRecipeOverviewQuery(owner, ids, requestedLimit), cancellation.Token);

        Assert.Multiple(
            () => Assert.Equal(1000, result.Total),
            () => Assert.Empty(result.Preview),
            () => Assert.Equal(ids[0].Value, Assert.Single(result.Items).RecipeId));
        await queries.Received(1).GetPageReadModelsAsync(owner, 1, expectedLimit, search: null, cancellation.Token);
        await queries.Received(1).GetByRecipeIdsReadModelsAsync(owner, ids, cancellation.Token);
        await queries.DidNotReceive().GetAllReadModelsAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>());
    }
}
