using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.RecipeCommunity.Application.RecipeComments.Models;

namespace FoodDiary.Modules.RecipeCommunity.Application.RecipeComments.Commands.UpdateRecipeComment;

public record UpdateRecipeCommentCommand(
    Guid? UserId,
    Guid RecipeId,
    Guid CommentId,
    string Text) : ICommand<Result<RecipeCommentModel>>, IUserRequest;
