using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;

namespace FoodDiary.Application.RecipeComments.Commands.DeleteRecipeComment;

public record DeleteRecipeCommentCommand(
    Guid? UserId,
    Guid RecipeId,
    Guid CommentId) : ICommand<Result>, IUserRequest;
