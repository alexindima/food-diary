using FoodDiary.Application.Common.Models;
using FoodDiary.Application.RecipeComments.Commands.CreateRecipeComment;
using FoodDiary.Application.RecipeComments.Commands.DeleteRecipeComment;
using FoodDiary.Application.RecipeComments.Commands.UpdateRecipeComment;
using FoodDiary.Application.RecipeComments.Models;
using FoodDiary.Application.RecipeComments.Queries.GetRecipeComments;
using FoodDiary.Presentation.Api.Features.RecipeComments.Mappings;
using FoodDiary.Presentation.Api.Features.RecipeComments.Requests;
using FoodDiary.Presentation.Api.Features.RecipeComments.Responses;
using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class RecipeCommentHttpMappingsTests {
    [Fact]
    public void ToQuery_MapsAllFields() {
        var userId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();

        GetRecipeCommentsQuery query = RecipeCommentHttpMappings.ToQuery(userId, recipeId, page: 2, limit: 30);

        Assert.Multiple(
            () => Assert.Equal(userId, query.UserId),
            () => Assert.Equal(recipeId, query.RecipeId),
            () => Assert.Equal(2, query.Page),
            () => Assert.Equal(30, query.Limit));
    }

    [Fact]
    public void CreateRecipeCommentRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();
        var request = new CreateRecipeCommentHttpRequest("Great recipe!");

        CreateRecipeCommentCommand command = request.ToCommand(userId, recipeId);

        Assert.Multiple(
            () => Assert.Equal(userId, command.UserId),
            () => Assert.Equal(recipeId, command.RecipeId),
            () => Assert.Equal("Great recipe!", command.Text));
    }

    [Fact]
    public void UpdateRecipeCommentRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var commentId = Guid.NewGuid();
        var request = new UpdateRecipeCommentHttpRequest("Updated text");

        UpdateRecipeCommentCommand command = request.ToCommand(userId, commentId);

        Assert.Multiple(
            () => Assert.Equal(userId, command.UserId),
            () => Assert.Equal(commentId, command.CommentId),
            () => Assert.Equal("Updated text", command.Text));
    }

    [Fact]
    public void RecipeCommentModel_ToHttpResponse_MapsAllFields() {
        var model = new RecipeCommentModel(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "johndoe", "John", "Looks delicious!",
            DateTime.UtcNow, DateTime.UtcNow, IsOwnedByCurrentUser: true);

        RecipeCommentHttpResponse response = model.ToHttpResponse();

        Assert.Multiple(
            () => Assert.Equal(model.Id, response.Id),
            () => Assert.Equal(model.RecipeId, response.RecipeId),
            () => Assert.Equal(model.AuthorId, response.AuthorId),
            () => Assert.Equal("johndoe", response.AuthorUsername),
            () => Assert.Equal("John", response.AuthorFirstName),
            () => Assert.Equal("Looks delicious!", response.Text),
            () => Assert.True(response.IsOwnedByCurrentUser));
    }

    [Fact]
    public void ToDeleteCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();
        var commentId = Guid.NewGuid();

        DeleteRecipeCommentCommand command = RecipeCommentHttpMappings.ToDeleteCommand(userId, recipeId, commentId);

        Assert.Multiple(
            () => Assert.Equal(userId, command.UserId),
            () => Assert.Equal(recipeId, command.RecipeId),
            () => Assert.Equal(commentId, command.CommentId));
    }

    [Fact]
    public void PagedRecipeCommentModel_ToHttpResponse_MapsPageAndData() {
        var model = new RecipeCommentModel(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "johndoe", "John", "Looks delicious!",
            DateTime.UtcNow, ModifiedAtUtc: null, IsOwnedByCurrentUser: true);
        var paged = new PagedResponse<RecipeCommentModel>(
            [model],
            Page: 2,
            Limit: 10,
            TotalPages: 3,
            TotalItems: 25);

        PagedHttpResponse<RecipeCommentHttpResponse> response = paged.ToHttpResponse();

        Assert.Multiple(
            () => Assert.Equal(2, response.Page),
            () => Assert.Equal(10, response.Limit),
            () => Assert.Equal(3, response.TotalPages),
            () => Assert.Equal(25, response.TotalItems));
        RecipeCommentHttpResponse item = Assert.Single(response.Data);
        Assert.Equal(model.Id, item.Id);
    }
}
