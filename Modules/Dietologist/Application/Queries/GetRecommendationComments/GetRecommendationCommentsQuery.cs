using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Dietologist.Application.Models;
using FoodDiary.Results;
using FoodDiary.Application.Contracts.Common.Models;

namespace FoodDiary.Modules.Dietologist.Application.Queries.GetRecommendationComments;

public sealed record GetRecommendationCommentsQuery(
    Guid? UserId,
    Guid RecommendationId,
    int Page = 1,
    int Limit = 50) : IQuery<Result<PagedResponse<RecommendationCommentModel>>>, IUserRequest;
