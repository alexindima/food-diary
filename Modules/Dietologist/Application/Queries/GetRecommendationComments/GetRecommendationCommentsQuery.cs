using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Dietologist.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Dietologist.Application.Queries.GetRecommendationComments;

public sealed record GetRecommendationCommentsQuery(
    Guid? UserId,
    Guid RecommendationId) : IQuery<Result<IReadOnlyList<RecommendationCommentModel>>>, IUserRequest;
