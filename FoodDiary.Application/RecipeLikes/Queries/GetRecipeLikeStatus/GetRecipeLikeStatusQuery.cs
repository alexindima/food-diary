using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.RecipeLikes.Models;

namespace FoodDiary.Application.RecipeLikes.Queries.GetRecipeLikeStatus;

public record GetRecipeLikeStatusQuery(
    Guid? UserId,
    Guid RecipeId) : IQuery<Result<RecipeLikeStatusModel>>, IUserRequest;
