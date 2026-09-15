using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Dietologist.Application.Models;

namespace FoodDiary.Modules.Dietologist.Application.Queries.GetRecommendationsForClient;

public record GetRecommendationsForClientQuery(
    Guid? UserId,
    Guid ClientUserId) : IQuery<Result<IReadOnlyList<RecommendationModel>>>, IUserRequest;
