using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.RecipeLikes.Models;

namespace FoodDiary.Application.RecipeLikes.Commands.ToggleRecipeLike;

public record ToggleRecipeLikeCommand(
    Guid? UserId,
    Guid RecipeId) : ICommand<Result<RecipeLikeStatusModel>>, IUserRequest;
