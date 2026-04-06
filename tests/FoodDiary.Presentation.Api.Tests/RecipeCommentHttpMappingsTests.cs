using FoodDiary.Application.RecipeComments.Models;
using FoodDiary.Presentation.Api.Features.RecipeComments.Mappings;
using FoodDiary.Presentation.Api.Features.RecipeComments.Requests;

namespace FoodDiary.Presentation.Api.Tests;

public sealed class RecipeCommentHttpMappingsTests {
    [Fact]
    public void CreateRecipeCommentRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();
        var request = new CreateRecipeCommentHttpRequest("Great recipe!");

        var command = request.ToCommand(userId, recipeId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(recipeId, command.RecipeId);
        Assert.Equal("Great recipe!", command.Text);
    }

    [Fact]
    public void UpdateRecipeCommentRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var commentId = Guid.NewGuid();
        var request = new UpdateRecipeCommentHttpRequest("Updated text");

        var command = request.ToCommand(userId, commentId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(commentId, command.CommentId);
        Assert.Equal("Updated text", command.Text);
    }

    [Fact]
    public void RecipeCommentModel_ToHttpResponse_MapsAllFields() {
        var model = new RecipeCommentModel(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "johndoe", "John", "Looks delicious!",
            DateTime.UtcNow, DateTime.UtcNow, true);

        var response = model.ToHttpResponse();

        Assert.Equal(model.Id, response.Id);
        Assert.Equal(model.RecipeId, response.RecipeId);
        Assert.Equal(model.AuthorId, response.AuthorId);
        Assert.Equal("johndoe", response.AuthorUsername);
        Assert.Equal("John", response.AuthorFirstName);
        Assert.Equal("Looks delicious!", response.Text);
        Assert.True(response.IsOwnedByCurrentUser);
    }
}
