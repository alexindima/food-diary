using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.RecipeLikes.Models;

namespace FoodDiary.Application.RecipeLikes.Commands.ToggleRecipeLike;

public record ToggleRecipeLikeCommand(
    Guid? UserId,
    Guid RecipeId) : ICommand<Result<RecipeLikeStatusModel>>, IUserRequest;
