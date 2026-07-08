using FoodDiary.Results;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Common;

public interface IDietologistRecommendationReadService {
    Task<Result<IReadOnlyList<RecommendationModel>>> GetForCurrentUserAsync(
        UserId userId,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<RecommendationModel>>> GetForClientAsync(
        UserId dietologistUserId,
        Guid clientUserId,
        CancellationToken cancellationToken);
}
