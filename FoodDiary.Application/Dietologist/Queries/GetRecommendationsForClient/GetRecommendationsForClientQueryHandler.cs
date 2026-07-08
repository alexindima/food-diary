using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Queries.GetRecommendationsForClient;

public sealed class GetRecommendationsForClientQueryHandler(IDietologistRecommendationReadService readService)
    : IQueryHandler<GetRecommendationsForClientQuery, Result<IReadOnlyList<RecommendationModel>>> {
    public async Task<Result<IReadOnlyList<RecommendationModel>>> Handle(
        GetRecommendationsForClientQuery query, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<IReadOnlyList<RecommendationModel>>(userIdResult);
        }

        UserId dietologistUserId = userIdResult.Value;
        return await readService.GetForClientAsync(dietologistUserId, query.ClientUserId, cancellationToken).ConfigureAwait(false);
    }
}
