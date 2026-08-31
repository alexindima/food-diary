using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.RecipeCommunity.RecipeComments.Commands.DeleteRecipeComment;

public record DeleteRecipeCommentCommand(
    Guid? UserId,
    Guid RecipeId,
    Guid CommentId) : ICommand<Result>, IUserRequest;
