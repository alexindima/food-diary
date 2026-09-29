using FoodDiary.Modules.Dietologist.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Common;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Models;
using FoodDiary.Modules.Dietologist.Application.Common.Validation;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Dietologist.Application.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Application.Contracts.Common.Models;
using FoodDiary.Application.Contracts.Common.Validation;

namespace FoodDiary.Modules.Dietologist.Application.Queries.GetRecommendationComments;

public sealed class GetRecommendationCommentsQueryHandler(
    IRecommendationCommentReadModelRepository commentRepository,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetRecommendationCommentsQuery, Result<PagedResponse<RecommendationCommentModel>>> {
    public async Task<Result<PagedResponse<RecommendationCommentModel>>> Handle(
        GetRecommendationCommentsQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId, currentUserAccessService, cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<PagedResponse<RecommendationCommentModel>>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        Guid recommendationId = query.RecommendationId;
        Result<RecommendationId> recommendationIdResult = DietologistRequiredIdParser.Parse(
            recommendationId,
            nameof(recommendationId),
            "Recommendation id must not be empty.",
            value => new RecommendationId(value));
        if (recommendationIdResult.IsFailure) {
            return Result.Failure<PagedResponse<RecommendationCommentModel>>(recommendationIdResult.Error);
        }

        bool isParticipant = await commentRepository.IsParticipantAsync(
            recommendationIdResult.Value, userId, cancellationToken).ConfigureAwait(false);
        if (!isParticipant) {
            return Result.Failure<PagedResponse<RecommendationCommentModel>>(DietologistErrors.InvitationNotFound);
        }

        int page = PaginationPolicy.NormalizePage(query.Page);
        int limit = PaginationPolicy.NormalizePageSize(query.Limit);
        (IReadOnlyList<RecommendationCommentReadModel> comments, int total) =
            await commentRepository.GetPageByRecommendationAsync(recommendationIdResult.Value, page, limit, cancellationToken).ConfigureAwait(false);
        int totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)limit);
        return Result.Success(new PagedResponse<RecommendationCommentModel>(
            comments.Select(ToModel).ToArray(), page, limit, totalPages, total));

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
