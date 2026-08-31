using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.RecipeCommunity.RecipeLikes.Models;

namespace FoodDiary.Application.RecipeCommunity.RecipeLikes.Queries.GetRecipeLikeStatus;

public record GetRecipeLikeStatusQuery(
    Guid? UserId,
    Guid RecipeId) : IQuery<Result<RecipeLikeStatusModel>>, IUserRequest;
