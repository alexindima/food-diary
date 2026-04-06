using FoodDiary.Application.RecipeLikes.Models;
using FoodDiary.Presentation.Api.Features.RecipeLikes.Mappings;

namespace FoodDiary.Presentation.Api.Tests;

public sealed class RecipeLikeHttpMappingsTests {
    [Fact]
    public void ToCommand_MapsUserIdAndRecipeId() {
        var userId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();

        var command = RecipeLikeHttpMappings.ToCommand(userId, recipeId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(recipeId, command.RecipeId);
    }

    [Fact]
    public void ToQuery_MapsUserIdAndRecipeId() {
        var userId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();

        var query = RecipeLikeHttpMappings.ToQuery(userId, recipeId);

        Assert.Equal(userId, query.UserId);
        Assert.Equal(recipeId, query.RecipeId);
    }

    [Fact]
    public void RecipeLikeStatusModel_ToHttpResponse_MapsAllFields() {
        var model = new RecipeLikeStatusModel(true, 42);

        var response = model.ToHttpResponse();

        Assert.True(response.IsLiked);
        Assert.Equal(42, response.TotalLikes);
    }

    [Fact]
    public void RecipeLikeStatusModel_ToHttpResponse_WhenNotLiked() {
        var model = new RecipeLikeStatusModel(false, 0);

        var response = model.ToHttpResponse();

        Assert.False(response.IsLiked);
        Assert.Equal(0, response.TotalLikes);
    }
}
