using FoodDiary.Modules.Recipes.Application.Queries.ExportCatalogRecipes;
using FoodDiary.Modules.Recipes.Application.Common;
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

    [Fact]
    public async Task Export_PreservesStepTextIngredientsAndImageUrlsWithoutEnvironmentSpecificAssetIds() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2, description: "Catalog recipe", prepTime: 5, cookTime: 20);
        recipe.AddStep(1, "Season", "Finish", "https://example.test/step.webp").AddTextIngredient("Salt", "a pinch");
        RecipeOverviewReadItem overview = TestRecipeOverview.From(recipe, UserId.Empty);
        RecipeOverviewStepReadItem step = Assert.Single(overview.Steps);
        RecipeOverviewIngredientReadItem ingredient = Assert.Single(step.Ingredients);
        overview = overview with {
            Language = "ru",
            LanguageConfirmed = true,
            Steps = [step with {
                ImageAssetId = Guid.NewGuid(),
                Ingredients = [ingredient with { TextName = "Salt", AmountText = "a pinch" }],
            }],
        };
        IRecipeOverviewReadService read = Substitute.For<IRecipeOverviewReadService>();
        using var cancellation = new CancellationTokenSource();
        read.GetPagedAsync(UserId.Empty, includePublic: true, 1, 100, Arg.Any<RecipeQueryFilters>(), cancellationToken: cancellation.Token)
            .Returns(((IReadOnlyList<RecipeOverviewReadItem>)[overview], 1));

        IReadOnlyList<CatalogRecipeModel> result = ResultAssert.Success(await new ExportCatalogRecipesQueryHandler(read)
            .Handle(new ExportCatalogRecipesQuery(), cancellation.Token));

        CatalogRecipeModel exported = Assert.Single(result);
        RecipeStepInput exportedStep = Assert.Single(exported.Steps);
        RecipeIngredientInput exportedIngredient = Assert.Single(exportedStep.Ingredients);
        Assert.Multiple(
            () => Assert.Equal(recipe.Id.Value, exported.Id),
            () => Assert.Equal("Soup", exported.Name),
            () => Assert.Equal("Catalog recipe", exported.Description),
            () => Assert.Equal(2, exported.Servings),
            () => Assert.Equal(5, exported.PrepTime),
            () => Assert.Equal(20, exported.CookTime),
            () => Assert.Equal("ru", exported.Language),
            () => Assert.True(exported.LanguageConfirmed),
            () => Assert.Equal(1, exportedStep.Order),
            () => Assert.Equal("Finish", exportedStep.Title),
            () => Assert.Equal("Season", exportedStep.Description),
            () => Assert.Equal("https://example.test/step.webp", exportedStep.ImageUrl),
            () => Assert.Null(exportedStep.ImageAssetId),
            () => Assert.Equal("Salt", exportedIngredient.TextName),
            () => Assert.Equal("a pinch", exportedIngredient.AmountText),
            () => Assert.Null(exportedIngredient.ProductId),
            () => Assert.Null(exportedIngredient.NestedRecipeId));
        Assert.Single(read.ReceivedCalls());
    }
}
