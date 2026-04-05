using FoodDiary.Application.Common.Models;
using FoodDiary.Application.RecipeComments.Commands.CreateRecipeComment;
using FoodDiary.Application.RecipeComments.Commands.DeleteRecipeComment;
using FoodDiary.Application.RecipeComments.Commands.UpdateRecipeComment;
using FoodDiary.Application.RecipeComments.Models;
using FoodDiary.Application.RecipeComments.Queries.GetRecipeComments;
using FoodDiary.Presentation.Api.Features.RecipeComments.Requests;
using FoodDiary.Presentation.Api.Features.RecipeComments.Responses;
using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Presentation.Api.Features.RecipeComments.Mappings;

public static class RecipeCommentHttpMappings {
    public static GetRecipeCommentsQuery ToQuery(Guid userId, Guid recipeId, int page, int limit) =>
        new(userId, recipeId, page, limit);

    public static CreateRecipeCommentCommand ToCommand(
        this CreateRecipeCommentHttpRequest request, Guid userId, Guid recipeId) =>
        new(userId, recipeId, request.Text);

    public static UpdateRecipeCommentCommand ToCommand(
        this UpdateRecipeCommentHttpRequest request, Guid userId, Guid commentId) =>
        new(userId, commentId, request.Text);

    public static DeleteRecipeCommentCommand ToDeleteCommand(Guid userId, Guid recipeId, Guid commentId) =>
        new(userId, recipeId, commentId);

    public static RecipeCommentHttpResponse ToHttpResponse(this RecipeCommentModel model) =>
        new(model.Id, model.RecipeId, model.AuthorId, model.AuthorUsername,
            model.AuthorFirstName, model.Text, model.CreatedAtUtc,
            model.ModifiedAtUtc, model.IsOwnedByCurrentUser);

    public static PagedHttpResponse<RecipeCommentHttpResponse> ToHttpResponse(
        this PagedResponse<RecipeCommentModel> response) =>
        response.ToPagedHttpResponse(ToHttpResponse);
}
