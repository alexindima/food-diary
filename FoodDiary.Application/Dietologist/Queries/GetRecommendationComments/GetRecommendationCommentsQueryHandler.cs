using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Dietologist.Queries.GetRecommendationComments;

public sealed class GetRecommendationCommentsQueryHandler(
    IRecommendationDiscussionReadService readService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetRecommendationCommentsQuery, Result<IReadOnlyList<RecommendationCommentModel>>> {
    public async Task<Result<IReadOnlyList<RecommendationCommentModel>>> Handle(
        GetRecommendationCommentsQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId, currentUserAccessService, cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<IReadOnlyList<RecommendationCommentModel>>(userIdResult);
        }

        return await readService.GetAsync(
            userIdResult.Value,
            query.RecommendationId,
            cancellationToken).ConfigureAwait(false);
    }
}
