using FoodDiary.Application.RecipeLikes.Commands.ToggleRecipeLike;
using FoodDiary.Application.RecipeLikes.Models;
using FoodDiary.Application.RecipeLikes.Queries.GetRecipeLikeStatus;
using FoodDiary.Presentation.Api.Features.RecipeLikes.Mappings;
using FoodDiary.Presentation.Api.Features.RecipeLikes.Responses;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class RecipeLikeHttpMappingsTests {
    [Fact]
    public void ToCommand_MapsUserIdAndRecipeId() {
        var userId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();

        ToggleRecipeLikeCommand command = RecipeLikeHttpMappings.ToCommand(userId, recipeId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(recipeId, command.RecipeId);
    }

    [Fact]
    public void ToQuery_MapsUserIdAndRecipeId() {
        var userId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();

        GetRecipeLikeStatusQuery query = RecipeLikeHttpMappings.ToQuery(userId, recipeId);

        Assert.Equal(userId, query.UserId);
        Assert.Equal(recipeId, query.RecipeId);
    }

    [Fact]
    public void RecipeLikeStatusModel_ToHttpResponse_MapsAllFields() {
        var model = new RecipeLikeStatusModel(IsLiked: true, 42);

        RecipeLikeStatusHttpResponse response = model.ToHttpResponse();

        Assert.True(response.IsLiked);
        Assert.Equal(42, response.TotalLikes);
    }

    [Fact]
    public void RecipeLikeStatusModel_ToHttpResponse_WhenNotLiked() {
        var model = new RecipeLikeStatusModel(IsLiked: false, 0);

        RecipeLikeStatusHttpResponse response = model.ToHttpResponse();

        Assert.False(response.IsLiked);
        Assert.Equal(0, response.TotalLikes);
    }
}
