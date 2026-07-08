using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Queries.GetRecommendationsForClient;

public sealed class GetRecommendationsForClientQueryHandler(
    IDietologistRecommendationReadService readService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetRecommendationsForClientQuery, Result<IReadOnlyList<RecommendationModel>>> {
    public async Task<Result<IReadOnlyList<RecommendationModel>>> Handle(
        GetRecommendationsForClientQuery query, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<IReadOnlyList<RecommendationModel>>(userIdResult);
        }

        UserId dietologistUserId = userIdResult.Value;
        return await readService.GetForClientAsync(dietologistUserId, query.ClientUserId, cancellationToken).ConfigureAwait(false);
    }
}
