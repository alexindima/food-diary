using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.RecipeCommunity.RecipeLikes.Models;

namespace FoodDiary.Application.RecipeCommunity.RecipeLikes.Commands.ToggleRecipeLike;

public record ToggleRecipeLikeCommand(
    Guid? UserId,
    Guid RecipeId) : ICommand<Result<RecipeLikeStatusModel>>, IUserRequest;
