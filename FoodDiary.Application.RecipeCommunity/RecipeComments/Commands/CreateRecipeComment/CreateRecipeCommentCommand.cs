using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.RecipeCommunity.RecipeComments.Models;

namespace FoodDiary.Application.RecipeCommunity.RecipeComments.Commands.CreateRecipeComment;

public record CreateRecipeCommentCommand(
    Guid? UserId,
    Guid RecipeId,
    string Text) : ICommand<Result<RecipeCommentModel>>, IUserRequest;
