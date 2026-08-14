using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.RecipeCommunity.RecipeComments.Models;

namespace FoodDiary.Application.RecipeCommunity.RecipeComments.Commands.UpdateRecipeComment;

public record UpdateRecipeCommentCommand(
    Guid? UserId,
    Guid CommentId,
    string Text) : ICommand<Result<RecipeCommentModel>>, IUserRequest;
