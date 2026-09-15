using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Dietologist.Application.Mappings;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Common;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Models;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Dietologist.Application.Common;
using FoodDiary.Modules.Dietologist.Application.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Dietologist.Application.Queries.GetMyRecommendations;

public sealed class GetMyRecommendationsQueryHandler(
    IRecommendationReadModelRepository recommendationRepository,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetMyRecommendationsQuery, Result<IReadOnlyList<RecommendationModel>>> {
    public async Task<Result<IReadOnlyList<RecommendationModel>>> Handle(
        GetMyRecommendationsQuery query, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<IReadOnlyList<RecommendationModel>>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        IReadOnlyList<RecommendationReadModel> recommendations = await recommendationRepository.GetByClientReadModelsAsync(userId, cancellationToken: cancellationToken).ConfigureAwait(false);
        var models = recommendations.Select(recommendation => recommendation.ToModel()).ToList();
        return Result.Success<IReadOnlyList<RecommendationModel>>(models);

    }

}
