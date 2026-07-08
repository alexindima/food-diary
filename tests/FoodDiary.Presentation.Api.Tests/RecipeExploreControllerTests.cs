using FoodDiary.Results;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Recipes.Models;
using FoodDiary.Application.Recipes.Queries.ExploreRecipes;
using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Features.Recipes;
using FoodDiary.Presentation.Api.Features.Recipes.Requests;
using FoodDiary.Presentation.Api.Features.Recipes.Responses;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class RecipeExploreControllerTests {
    [Fact]
    public async Task Explore_SendsExploreRecipesQueryAndReturnsPagedResponse() {
        RecipeModel recipe = CreateRecipe();
        IRequest<Result<PagedResponse<RecipeModel>>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(
            Result.Success(new PagedResponse<RecipeModel>([recipe], Page: 2, Limit: 5, TotalPages: 3, TotalItems: 12)),
            request => sentRequest = request);
        RecipeExploreController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        ExploreRecipesHttpQuery query = new(
            Page: 2,
            Limit: 5,
            Search: "soup",
            Category: "lunch",
            MaxPrepTime: 20,
            SortBy: "popular");

        IActionResult result = await controller.Explore(userId, query);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        PagedHttpResponse<RecipeHttpResponse> response = Assert.IsType<PagedHttpResponse<RecipeHttpResponse>>(ok.Value);
        Assert.Equal(2, response.Page);
        Assert.Equal(5, response.Limit);
        Assert.Equal(3, response.TotalPages);
        Assert.Equal(12, response.TotalItems);
        RecipeHttpResponse item = Assert.Single(response.Data);
        Assert.Equal(recipe.Id, item.Id);
        Assert.Equal(recipe.Name, item.Name);

        ExploreRecipesQuery sentQuery = Assert.IsType<ExploreRecipesQuery>(sentRequest);
        Assert.Equal(userId, sentQuery.UserId);
        Assert.Equal(2, sentQuery.Page);
        Assert.Equal(5, sentQuery.Limit);
        Assert.Equal("soup", sentQuery.Search);
        Assert.Equal("lunch", sentQuery.Category);
        Assert.Equal(20, sentQuery.MaxPrepTime);
        Assert.Equal("popular", sentQuery.SortBy);
    }

    private static RecipeExploreController CreateController(ISender sender) =>
        new(sender) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext(),
            },
        };

    private static RecipeModel CreateRecipe() =>
        new(
            Guid.NewGuid(),
            "Tomato soup",
            Description: "Rich tomato soup",
            Comment: null,
            Category: "Lunch",
            ImageUrl: null,
            ImageAssetId: null,
            PrepTime: 10,
            CookTime: 20,
            Servings: 2,
            TotalCalories: 250,
            TotalProteins: 8,
            TotalFats: 7,
            TotalCarbs: 35,
            TotalFiber: 6,
            TotalAlcohol: 0,
            IsNutritionAutoCalculated: true,
            ManualCalories: null,
            ManualProteins: null,
            ManualFats: null,
            ManualCarbs: null,
            ManualFiber: null,
            ManualAlcohol: null,
            Visibility: "Public",
            UsageCount: 4,
            CreatedAt: DateTime.UtcNow,
            IsOwnedByCurrentUser: false,
            QualityScore: 80,
            QualityGrade: "green",
            Steps: [],
            IsFavorite: true,
            FavoriteRecipeId: Guid.NewGuid());

}
