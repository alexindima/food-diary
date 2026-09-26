using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.RecipeCommunity.Application.RecipeComments.Commands.DeleteRecipeComment;

public record DeleteRecipeCommentCommand(
    Guid? UserId,
    Guid RecipeId,
    Guid CommentId) : ICommand<Result>, IUserRequest;
