using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.FavoriteRecipes.Commands.AddFavoriteRecipe;
using FoodDiary.Application.FavoriteRecipes.Commands.RemoveFavoriteRecipe;
using FoodDiary.Application.FavoriteRecipes.Models;
using FoodDiary.Application.FavoriteRecipes.Queries.GetFavoriteRecipes;
using FoodDiary.Application.FavoriteRecipes.Queries.IsRecipeFavorite;
using FoodDiary.Presentation.Api.Features.FavoriteRecipes;
using FoodDiary.Presentation.Api.Features.FavoriteRecipes.Requests;
using FoodDiary.Presentation.Api.Features.FavoriteRecipes.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class FavoriteRecipesControllerTests {
    [Fact]
    public async Task GetAll_SendsQueryAndReturnsFavorites() {
        FavoriteRecipeModel favorite = CreateFavorite();
        RecordingSender sender = new(Result.Success<IReadOnlyList<FavoriteRecipeModel>>([favorite]));
        FavoriteRecipesController controller = CreateController(sender);
        var userId = Guid.NewGuid();

        IActionResult result = await controller.GetAll(userId);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        List<FavoriteRecipeHttpResponse> response = Assert.IsType<List<FavoriteRecipeHttpResponse>>(ok.Value);
        Assert.Single(response);
        Assert.Equal(favorite.Id, response[0].Id);
        GetFavoriteRecipesQuery query = Assert.IsType<GetFavoriteRecipesQuery>(sender.Request);
        Assert.Equal(userId, query.UserId);
    }

    [Fact]
    public async Task IsFavorite_SendsQueryAndReturnsFlag() {
        RecordingSender sender = new(Result.Success(value: true));
        FavoriteRecipesController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();

        IActionResult result = await controller.IsFavorite(recipeId, userId);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(true, ok.Value);
        IsRecipeFavoriteQuery query = Assert.IsType<IsRecipeFavoriteQuery>(sender.Request);
        Assert.Equal(userId, query.UserId);
        Assert.Equal(recipeId, query.RecipeId);
    }

    [Fact]
    public async Task Add_SendsCommandAndReturnsFavorite() {
        FavoriteRecipeModel favorite = CreateFavorite();
        RecordingSender sender = new(Result.Success(favorite));
        FavoriteRecipesController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();
        var request = new AddFavoriteRecipeHttpRequest(recipeId, "Dinner");

        IActionResult result = await controller.Add(userId, request);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        FavoriteRecipeHttpResponse response = Assert.IsType<FavoriteRecipeHttpResponse>(ok.Value);
        Assert.Equal(favorite.Id, response.Id);
        AddFavoriteRecipeCommand command = Assert.IsType<AddFavoriteRecipeCommand>(sender.Request);
        Assert.Equal(userId, command.UserId);
        Assert.Equal(recipeId, command.RecipeId);
        Assert.Equal("Dinner", command.Name);
    }

    [Fact]
    public async Task Remove_SendsCommandAndReturnsNoContent() {
        RecordingSender sender = new(Result.Success());
        FavoriteRecipesController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        var favoriteRecipeId = Guid.NewGuid();

        IActionResult result = await controller.Remove(favoriteRecipeId, userId);

        Assert.IsType<NoContentResult>(result);
        RemoveFavoriteRecipeCommand command = Assert.IsType<RemoveFavoriteRecipeCommand>(sender.Request);
        Assert.Equal(userId, command.UserId);
        Assert.Equal(favoriteRecipeId, command.FavoriteRecipeId);
    }

    private static FavoriteRecipesController CreateController(RecordingSender sender) =>
        new(sender) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext(),
            },
        };

    private static FavoriteRecipeModel CreateFavorite() =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Dinner",
            DateTime.UtcNow.AddDays(-1),
            "Soup",
            "https://cdn.example/soup.png",
            TotalCalories: 320,
            Servings: 2,
            TotalTimeMinutes: 25,
            IngredientCount: 7);
}
