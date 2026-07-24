using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Dietologist.Queries.GetRecommendationComments;

public sealed record GetRecommendationCommentsQuery(
    Guid? UserId,
    Guid RecommendationId) : IQuery<Result<IReadOnlyList<RecommendationCommentModel>>>, IUserRequest;
