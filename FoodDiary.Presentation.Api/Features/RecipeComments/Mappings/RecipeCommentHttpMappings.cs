using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Application.RecipeCommunity.RecipeComments.Commands.CreateRecipeComment;
using FoodDiary.Application.RecipeCommunity.RecipeComments.Commands.DeleteRecipeComment;
using FoodDiary.Application.RecipeCommunity.RecipeComments.Commands.UpdateRecipeComment;
using FoodDiary.Application.RecipeCommunity.RecipeComments.Models;
using FoodDiary.Application.RecipeCommunity.RecipeComments.Queries.GetRecipeComments;
using FoodDiary.Presentation.Api.Features.RecipeComments.Requests;
using FoodDiary.Presentation.Api.Features.RecipeComments.Responses;
using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Presentation.Api.Features.RecipeComments.Mappings;

public static class RecipeCommentHttpMappings {
    public static GetRecipeCommentsQuery ToQuery(Guid userId, Guid recipeId, int page, int limit) =>
        new(userId, recipeId, page, limit);

    extension(CreateRecipeCommentHttpRequest request) {
        public CreateRecipeCommentCommand ToCommand(
        Guid userId, Guid recipeId) =>
                new(userId, recipeId, request.Text);
    }

    extension(UpdateRecipeCommentHttpRequest request) {
        public UpdateRecipeCommentCommand ToCommand(
        Guid userId, Guid commentId) =>
                new(userId, commentId, request.Text);
    }

    public static DeleteRecipeCommentCommand ToDeleteCommand(Guid userId, Guid recipeId, Guid commentId) =>
        new(userId, recipeId, commentId);

    extension(RecipeCommentModel model) {
        public RecipeCommentHttpResponse ToHttpResponse() =>
                new(model.Id, model.RecipeId, model.AuthorId, model.AuthorUsername,
                    model.AuthorFirstName, model.Text, model.CreatedAtUtc,
                    model.ModifiedAtUtc, model.IsOwnedByCurrentUser);
    }

    extension(PagedResponse<RecipeCommentModel> response) {
        public PagedHttpResponse<RecipeCommentHttpResponse> ToHttpResponse(
        ) =>
                response.ToPagedHttpResponse(ToHttpResponse);
    }
}
