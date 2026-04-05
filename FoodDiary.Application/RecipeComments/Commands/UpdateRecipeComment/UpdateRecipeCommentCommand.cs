using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.RecipeComments.Models;

namespace FoodDiary.Application.RecipeComments.Commands.UpdateRecipeComment;

public record UpdateRecipeCommentCommand(
    Guid? UserId,
    Guid CommentId,
    string Text) : ICommand<Result<RecipeCommentModel>>, IUserRequest;
