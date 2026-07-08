using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Dietologist.Models;

namespace FoodDiary.Application.Dietologist.Queries.GetRecommendationsForClient;

public record GetRecommendationsForClientQuery(
    Guid? UserId,
    Guid ClientUserId) : IQuery<Result<IReadOnlyList<RecommendationModel>>>, IUserRequest;
