using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.RecipeCommunity.Application.RecipeLikes.Models;

namespace FoodDiary.Modules.RecipeCommunity.Application.RecipeLikes.Queries.GetRecipeLikeStatus;

public record GetRecipeLikeStatusQuery(
    Guid? UserId,
    Guid RecipeId) : IQuery<Result<RecipeLikeStatusModel>>, IUserRequest;
