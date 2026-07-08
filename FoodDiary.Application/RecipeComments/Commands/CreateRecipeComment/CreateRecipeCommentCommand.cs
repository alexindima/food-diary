using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.RecipeComments.Models;

namespace FoodDiary.Application.RecipeComments.Commands.CreateRecipeComment;

public record CreateRecipeCommentCommand(
    Guid? UserId,
    Guid RecipeId,
    string Text) : ICommand<Result<RecipeCommentModel>>, IUserRequest;
