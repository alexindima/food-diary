using FoodDiary.Application.Contracts.Common.Models;
using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Recipes.Application.Mappings;
using FoodDiary.Modules.Recipes.Application.Models;
using FoodDiary.Modules.Recipes.Application.Queries.GetPublicRecipe;
using FoodDiary.Modules.Recipes.Application.Queries.GetPublicRecipes;
using FoodDiary.Modules.Recipes.Contracts.Common;
using FoodDiary.Modules.Recipes.Contracts.Models;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using ResultAssert = FoodDiary.Testing.Assertions.ResultAssert;
using FoodDiary.Results;

namespace FoodDiary.Modules.Recipes.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class PublicRecipeQueryTests {
    [Theory]
    [InlineData("newest", true)]
    [InlineData("oldest", true)]
    [InlineData("slowest", true)]
    [InlineData("name_desc", true)]
    [InlineData("fastest", true)]
    [InlineData("name", true)]
    [InlineData("popular", false)]
    [InlineData("", false)]
    public void PublicCatalog_ValidatesSortOrder(string sortBy, bool valid) {
        var validator = new GetPublicRecipesQueryValidator();
        Assert.Equal(valid, validator.Validate(new GetPublicRecipesQuery(1, 20, Search: null, Category: null, MaxTotalTime: null, SortBy: sortBy)).IsValid);
    }

    [Fact]
    public void Projection_PreservesGalleryOrderAndPrefersStepGalleryOverLegacyCover() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2);
        RecipeImageReadItem[] images = [new(Guid.NewGuid(), "https://test/second"), new(Guid.NewGuid(), "https://test/first")];
        RecipeOverviewReadItem overview = TestRecipeOverview.From(recipe, recipe.UserId) with {
            AuthorName = "Dima",
            Images = images,
            Steps = [new RecipeOverviewStepReadItem(Guid.NewGuid(), 1, Title: null, "Cook", "https://test/legacy", ImageAssetId: null, []) { Images = images }],
        };
        PublicRecipeModel model = overview.ToPublicModel();
        Assert.Equal("Dima", model.AuthorName);
        Assert.Equal(images.Select(image => image.ImageUrl), model.Images, StringComparer.Ordinal);
        Assert.Equal(images.Select(image => image.ImageUrl), Assert.Single(model.Steps).Images, StringComparer.Ordinal);
        PublicRecipeModel noImages = (overview with { Steps = [overview.Steps[0] with { Images = [], ImageUrl = null }] }).ToPublicModel();
        Assert.Empty(Assert.Single(noImages.Steps).Images);
    }

    [Theory]
    [InlineData(Visibility.Private, false)]
    [InlineData(Visibility.Public, true)]
    public async Task Detail_AlwaysUsesAnonymousVisibility(Visibility visibility, bool expectedSuccess) {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2, visibility: visibility);
        IRecipeOverviewReadService read = Substitute.For<IRecipeOverviewReadService>();
        read.GetByIdsWithUsageAsync(Arg.Any<IEnumerable<RecipeId>>(), UserId.Empty, includePublic: true, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<RecipeId, RecipeOverviewReadItem> { [recipe.Id] = TestRecipeOverview.From(recipe, recipe.UserId) });
        GetPublicRecipeQueryHandler handler = new(read);

        Result<PublicRecipeModel> result = await handler.Handle(new GetPublicRecipeQuery(recipe.Id.Value), CancellationToken.None);

        Assert.Equal(expectedSuccess, result.IsSuccess);
        await read.Received(1).GetByIdsWithUsageAsync(Arg.Any<IEnumerable<RecipeId>>(), UserId.Empty, includePublic: true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Detail_EmptyIdIsNotFoundWithoutQuery() {
        IRecipeOverviewReadService read = Substitute.For<IRecipeOverviewReadService>();
        GetPublicRecipeQueryHandler handler = new(read);
        Result<PublicRecipeModel> result = await handler.Handle(new GetPublicRecipeQuery(Guid.Empty), CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Empty(read.ReceivedCalls());
    }

    [Fact]
    public async Task Catalog_PassesPaginationAndTotalTimeWithoutCurrentUser() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2);
        IRecipeOverviewReadService read = Substitute.For<IRecipeOverviewReadService>();
        read.GetPagedAsync(UserId.Empty, includePublic: true, 2, 20, Arg.Any<RecipeQueryFilters>(), Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<RecipeOverviewReadItem>)[TestRecipeOverview.From(recipe, UserId.Empty)], 41));

        Result<PagedResponse<PublicRecipeModel>> result = await new GetPublicRecipesQueryHandler(read).Handle(new GetPublicRecipesQuery(2, 20, "Soup", "Dinner", 30, "fastest"), CancellationToken.None);

        PagedResponse<PublicRecipeModel> page = ResultAssert.Success(result);
        Assert.Multiple(() => Assert.Equal(3, page.TotalPages), () => Assert.Equal(41, page.TotalItems), () => Assert.Single(page.Data));
        await read.Received(1).GetPagedAsync(UserId.Empty, includePublic: true, 2, 20,
            Arg.Is<RecipeQueryFilters>(filter => filter.Search == "Soup" && filter.Category == "Dinner" && filter.MaxTotalTime == 30 && filter.SortBy == "fastest"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Projection_HidesPrivateIngredientDetailsButPreservesTextAndGallery() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2, comment: "secret note");
        RecipeOverviewIngredientReadItem source = new(Id: Guid.NewGuid(), Amount: 100, ProductId: Guid.NewGuid(), ProductName: "Secret product", ProductBaseUnit: "g", ProductBaseAmount: 100,
            ProductCaloriesPerBase: 42, ProductProteinsPerBase: 1, ProductFatsPerBase: 2, ProductCarbsPerBase: 3, ProductFiberPerBase: 4, ProductAlcoholPerBase: 0,
            NestedRecipeId: null, NestedRecipeName: null, NestedRecipeServings: null, NestedRecipeTotalCalories: null, NestedRecipeTotalProteins: null,
            NestedRecipeTotalFats: null, NestedRecipeTotalCarbs: null, NestedRecipeTotalFiber: null, NestedRecipeTotalAlcohol: null, ProductIsAccessible: false);
        RecipeOverviewReadItem overview = TestRecipeOverview.From(recipe, recipe.UserId) with {
            MissingIngredientCount = 2,
            Steps = [new RecipeOverviewStepReadItem(Guid.NewGuid(), 1, "Cook", "Boil", "https://example.com/step.jpg", Guid.NewGuid(),
                [source, source with { ProductId = null, ProductIsAccessible = true, TextName = "Salt", AmountText = "to taste" },
                    source with { ProductId = null, TextName = "Hidden seasoning" }])],
        };

        PublicRecipeModel result = overview.ToPublicModel();
        PublicRecipeIngredientModel hidden = result.Steps[0].Ingredients[0];
        PublicRecipeIngredientModel text = result.Steps[0].Ingredients[1];
        Assert.Multiple(
            () => Assert.False(hidden.IsAvailable), () => Assert.Null(hidden.Name), () => Assert.Null(hidden.Amount),
            () => Assert.Null(hidden.Unit), () => Assert.Null(hidden.RecipeId),
            () => Assert.Equal("Salt", text.Name), () => Assert.Equal("to taste", text.AmountText), () => Assert.Null(text.Amount),
            () => Assert.Equal("https://example.com/step.jpg", Assert.Single(result.Steps[0].Images)));
        Assert.Equal("Salt", Assert.Single(result.MissingIngredientNames));
        Assert.Empty((overview with { MissingIngredientCount = 0 }).ToPublicModel().MissingIngredientNames);
    }
}
