using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.RecipeComments.Commands.CreateRecipeComment;
using FoodDiary.Application.RecipeComments.Commands.DeleteRecipeComment;
using FoodDiary.Application.RecipeComments.Commands.UpdateRecipeComment;
using FoodDiary.Application.RecipeComments.Models;
using FoodDiary.Application.RecipeComments.Queries.GetRecipeComments;
using FoodDiary.Presentation.Api.Features.RecipeComments;
using FoodDiary.Presentation.Api.Features.RecipeComments.Requests;
using FoodDiary.Presentation.Api.Features.RecipeComments.Responses;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class RecipeCommentsControllerTests {
    [Fact]
    public async Task GetAll_SendsQueryAndReturnsPagedComments() {
        RecipeCommentModel comment = CreateComment();
        var paged = new PagedResponse<RecipeCommentModel>([comment], Page: 2, Limit: 10, TotalPages: 3, TotalItems: 25);
        RecordingSender sender = new(Result.Success(paged));
        RecipeCommentsController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();

        IActionResult result = await controller.GetAll(userId, recipeId, page: 2, limit: 10);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        PagedHttpResponse<RecipeCommentHttpResponse> response = Assert.IsType<PagedHttpResponse<RecipeCommentHttpResponse>>(ok.Value);
        Assert.Equal(2, response.Page);
        Assert.Single(response.Data);
        GetRecipeCommentsQuery query = Assert.IsType<GetRecipeCommentsQuery>(sender.Request);
        Assert.Equal(userId, query.UserId);
        Assert.Equal(recipeId, query.RecipeId);
        Assert.Equal(2, query.Page);
        Assert.Equal(10, query.Limit);
    }

    [Fact]
    public async Task Create_SendsCommandAndReturnsCreatedComment() {
        RecipeCommentModel comment = CreateComment();
        RecordingSender sender = new(Result.Success(comment));
        RecipeCommentsController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();
        var request = new CreateRecipeCommentHttpRequest("Great recipe!");

        IActionResult result = await controller.Create(userId, recipeId, request);

        CreatedResult created = Assert.IsType<CreatedResult>(result);
        RecipeCommentHttpResponse response = Assert.IsType<RecipeCommentHttpResponse>(created.Value);
        Assert.Equal(comment.Id, response.Id);
        CreateRecipeCommentCommand command = Assert.IsType<CreateRecipeCommentCommand>(sender.Request);
        Assert.Equal(userId, command.UserId);
        Assert.Equal(recipeId, command.RecipeId);
        Assert.Equal("Great recipe!", command.Text);
    }

    [Fact]
    public async Task Update_SendsCommandAndReturnsComment() {
        RecipeCommentModel comment = CreateComment();
        RecordingSender sender = new(Result.Success(comment));
        RecipeCommentsController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();
        var commentId = Guid.NewGuid();
        var request = new UpdateRecipeCommentHttpRequest("Updated text");

        IActionResult result = await controller.Update(userId, recipeId, commentId, request);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        RecipeCommentHttpResponse response = Assert.IsType<RecipeCommentHttpResponse>(ok.Value);
        Assert.Equal(comment.Id, response.Id);
        UpdateRecipeCommentCommand command = Assert.IsType<UpdateRecipeCommentCommand>(sender.Request);
        Assert.Equal(userId, command.UserId);
        Assert.Equal(commentId, command.CommentId);
        Assert.Equal("Updated text", command.Text);
    }

    [Fact]
    public async Task Delete_SendsCommandAndReturnsNoContent() {
        RecordingSender sender = new(Result.Success());
        RecipeCommentsController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();
        var commentId = Guid.NewGuid();

        IActionResult result = await controller.Delete(userId, recipeId, commentId);

        Assert.IsType<NoContentResult>(result);
        DeleteRecipeCommentCommand command = Assert.IsType<DeleteRecipeCommentCommand>(sender.Request);
        Assert.Equal(userId, command.UserId);
        Assert.Equal(recipeId, command.RecipeId);
        Assert.Equal(commentId, command.CommentId);
    }

    private static RecipeCommentsController CreateController(RecordingSender sender) =>
        new(sender) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext(),
            },
        };

    private static RecipeCommentModel CreateComment() =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "johndoe",
            "John",
            "Looks delicious!",
            DateTime.UtcNow,
            ModifiedAtUtc: null,
            IsOwnedByCurrentUser: true);
}
