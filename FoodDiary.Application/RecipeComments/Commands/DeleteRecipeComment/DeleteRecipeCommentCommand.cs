using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;

namespace FoodDiary.Application.RecipeComments.Commands.DeleteRecipeComment;

public record DeleteRecipeCommentCommand(
    Guid? UserId,
    Guid RecipeId,
    Guid CommentId) : ICommand<Result>, IUserRequest;
