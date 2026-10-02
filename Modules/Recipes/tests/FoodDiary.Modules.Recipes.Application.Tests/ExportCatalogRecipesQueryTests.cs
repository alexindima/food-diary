using FoodDiary.Modules.Recipes.Application.Queries.ExportCatalogRecipes;
using FoodDiary.Modules.Recipes.Contracts.Common;
using FoodDiary.Modules.Recipes.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Recipes.Application.Models;
using FoodDiary.Results;
using ResultAssert = FoodDiary.Testing.Assertions.ResultAssert;
using FoodDiary.Modules.Recipes.Domain.Entities;

namespace FoodDiary.Modules.Recipes.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class ExportCatalogRecipesQueryTests {
    [Fact]
    public async Task ExportAtLimitReadsBoundedPublicPagesAsync() {
        RecipeOverviewReadItem item = TestRecipeOverview.From(Recipe.Create(UserId.New(), "Soup", 2), UserId.Empty);
        IReadOnlyList<RecipeOverviewReadItem> items = Enumerable.Repeat(item, 100).ToArray();
        IRecipeOverviewReadService read = Substitute.For<IRecipeOverviewReadService>();
        read.GetPagedAsync(UserId.Empty, includePublic: true, Arg.Any<int>(), 100, Arg.Any<RecipeQueryFilters>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns((items, 5000));
        using var cancellation = new CancellationTokenSource();

        Result<IReadOnlyList<CatalogRecipeModel>> result = await new ExportCatalogRecipesQueryHandler(read).Handle(new ExportCatalogRecipesQuery(), cancellationToken: cancellation.Token);

        Assert.Equal(5000, ResultAssert.Success(result).Count);
        await read.Received(50).GetPagedAsync(UserId.Empty, includePublic: true, Arg.Any<int>(), 100,
            Arg.Is<RecipeQueryFilters>(filter => filter.Search == null), cancellationToken: cancellation.Token);
        int[] pages = [.. read.ReceivedCalls().Select(call => (int)call.GetArguments()[2]!)];
        Assert.Equal(Enumerable.Range(1, 50), pages);
    }

    [Fact]
    public async Task ExportAboveLimitFailsWithoutContinuingAsync() {
        IRecipeOverviewReadService read = Substitute.For<IRecipeOverviewReadService>();
        read.GetPagedAsync(UserId.Empty, includePublic: true, 1, 100, Arg.Any<RecipeQueryFilters>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<RecipeOverviewReadItem>)[], 5001));

        Result<IReadOnlyList<CatalogRecipeModel>> result = await new ExportCatalogRecipesQueryHandler(read).Handle(new ExportCatalogRecipesQuery(), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Single(read.ReceivedCalls());
    }
}
