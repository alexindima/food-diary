using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Dietologist.Mappings;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Domain.Entities.Dietologist;

namespace FoodDiary.Application.Dietologist.Queries.GetMyRecommendations;

public class GetMyRecommendationsQueryHandler(
    IRecommendationRepository recommendationRepository,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetMyRecommendationsQuery, Result<IReadOnlyList<RecommendationModel>>> {
    public async Task<Result<IReadOnlyList<RecommendationModel>>> Handle(
        GetMyRecommendationsQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<IReadOnlyList<RecommendationModel>>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId!.Value);
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<IReadOnlyList<RecommendationModel>>(accessError);
        }

        IReadOnlyList<Recommendation> recommendations = await recommendationRepository.GetByClientAsync(userId, cancellationToken: cancellationToken).ConfigureAwait(false);
        var models = recommendations.Select(r => r.ToModel()).ToList();
        return Result.Success<IReadOnlyList<RecommendationModel>>(models);
    }
}
