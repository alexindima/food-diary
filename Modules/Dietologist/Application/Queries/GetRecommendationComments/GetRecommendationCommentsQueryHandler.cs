using FoodDiary.Modules.Dietologist.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Common;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Models;
using FoodDiary.Modules.Dietologist.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Dietologist.Application.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Dietologist.Application.Queries.GetRecommendationComments;

public sealed class GetRecommendationCommentsQueryHandler(
    IRecommendationCommentReadModelRepository commentRepository,
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

        UserId userId = userIdResult.Value;
        Guid recommendationId = query.RecommendationId;
        Result<RecommendationId> recommendationIdResult = DietologistRequiredIdParser.Parse(
            recommendationId,
            nameof(recommendationId),
            "Recommendation id must not be empty.",
            value => new RecommendationId(value));
        if (recommendationIdResult.IsFailure) {
            return Result.Failure<IReadOnlyList<RecommendationCommentModel>>(recommendationIdResult.Error);
        }

        bool isParticipant = await commentRepository.IsParticipantAsync(
            recommendationIdResult.Value, userId, cancellationToken).ConfigureAwait(false);
        if (!isParticipant) {
            return Result.Failure<IReadOnlyList<RecommendationCommentModel>>(DietologistErrors.InvitationNotFound);
        }

        IReadOnlyList<RecommendationCommentReadModel> comments =
            await commentRepository.GetByRecommendationAsync(recommendationIdResult.Value, cancellationToken).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<RecommendationCommentModel>>(
            comments.Select(ToModel).ToList());

    }

    private static RecommendationCommentModel ToModel(RecommendationCommentReadModel comment) =>
        new(
            comment.Id,
            comment.RecommendationId,
            comment.AuthorUserId,
            comment.AuthorFirstName,
            comment.AuthorLastName,
            comment.AuthorEmail,
            comment.Text,
            comment.CreatedAtUtc);
}
