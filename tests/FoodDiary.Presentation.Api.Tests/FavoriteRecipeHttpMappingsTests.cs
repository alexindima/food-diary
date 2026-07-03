using FoodDiary.Application.FavoriteRecipes.Commands.AddFavoriteRecipe;
using FoodDiary.Application.FavoriteRecipes.Commands.RemoveFavoriteRecipe;
using FoodDiary.Application.FavoriteRecipes.Models;
using FoodDiary.Application.FavoriteRecipes.Queries.GetFavoriteRecipes;
using FoodDiary.Application.FavoriteRecipes.Queries.IsRecipeFavorite;
using FoodDiary.Presentation.Api.Features.FavoriteRecipes.Mappings;
using FoodDiary.Presentation.Api.Features.FavoriteRecipes.Requests;
using FoodDiary.Presentation.Api.Features.FavoriteRecipes.Responses;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class FavoriteRecipeHttpMappingsTests {
    [Fact]
    public void AddFavoriteRecipeRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();
        var request = new AddFavoriteRecipeHttpRequest(recipeId, "Dinner");

        AddFavoriteRecipeCommand command = request.ToCommand(userId);

        Assert.Multiple(
            () => Assert.Equal(userId, command.UserId),
            () => Assert.Equal(recipeId, command.RecipeId),
            () => Assert.Equal("Dinner", command.Name));
    }

    [Fact]
    public void FavoriteRecipeId_ToQueriesAndDeleteCommand_MapsIds() {
        var userId = Guid.NewGuid();
        var favoriteRecipeId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();

        RemoveFavoriteRecipeCommand delete = favoriteRecipeId.ToDeleteCommand(userId);
        GetFavoriteRecipesQuery query = userId.ToQuery();
        IsRecipeFavoriteQuery favoriteQuery = recipeId.ToIsFavoriteQuery(userId);

        Assert.Multiple(
            () => Assert.Equal(userId, delete.UserId),
            () => Assert.Equal(favoriteRecipeId, delete.FavoriteRecipeId),
            () => Assert.Equal(userId, query.UserId),
            () => Assert.Equal(userId, favoriteQuery.UserId),
            () => Assert.Equal(recipeId, favoriteQuery.RecipeId));
    }

    [Fact]
    public void FavoriteRecipeModel_ToHttpResponse_MapsAllFields() {
        DateTime createdAtUtc = DateTime.UtcNow.AddDays(-1);
        var model = new FavoriteRecipeModel(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Dinner",
            createdAtUtc,
            "Soup",
            "https://cdn.example/soup.png",
            TotalCalories: 320,
            Servings: 2,
            TotalTimeMinutes: 25,
            IngredientCount: 7);

        FavoriteRecipeHttpResponse response = model.ToHttpResponse();

        Assert.Multiple(
            () => Assert.Equal(model.Id, response.Id),
            () => Assert.Equal(model.RecipeId, response.RecipeId),
            () => Assert.Equal("Dinner", response.Name),
            () => Assert.Equal(createdAtUtc, response.CreatedAtUtc),
            () => Assert.Equal("Soup", response.RecipeName),
            () => Assert.Equal("https://cdn.example/soup.png", response.ImageUrl),
            () => Assert.Equal(320, response.TotalCalories),
            () => Assert.Equal(2, response.Servings),
            () => Assert.Equal(25, response.TotalTimeMinutes),
            () => Assert.Equal(7, response.IngredientCount));
    }
}
