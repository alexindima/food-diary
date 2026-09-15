using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Modules.RecipeCommunity.Application.RecipeComments.Commands.CreateRecipeComment;
using FoodDiary.Modules.RecipeCommunity.Application.RecipeComments.Commands.DeleteRecipeComment;
using FoodDiary.Modules.RecipeCommunity.Application.RecipeComments.Commands.UpdateRecipeComment;
using FoodDiary.Modules.RecipeCommunity.Application.RecipeComments.Models;
using FoodDiary.Modules.RecipeCommunity.Application.RecipeComments.Queries.GetRecipeComments;
using FoodDiary.Modules.RecipeCommunity.Presentation.RecipeComments.Requests;
using FoodDiary.Modules.RecipeCommunity.Presentation.RecipeComments.Responses;
using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Modules.RecipeCommunity.Presentation.RecipeComments.Mappings;

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
        Guid userId, Guid recipeId, Guid commentId) =>
                new(userId, recipeId, commentId, request.Text);
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
