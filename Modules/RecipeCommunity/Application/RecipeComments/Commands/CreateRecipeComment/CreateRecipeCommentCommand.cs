using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.RecipeCommunity.Application.RecipeComments.Models;

namespace FoodDiary.Modules.RecipeCommunity.Application.RecipeComments.Commands.CreateRecipeComment;

public record CreateRecipeCommentCommand(
    Guid? UserId,
    Guid RecipeId,
    string Text) : ICommand<Result<RecipeCommentModel>>, IUserRequest;
