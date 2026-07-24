using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Dietologist.Services;

public sealed class RecommendationDiscussionReadService(
    IRecommendationReadRepository recommendationRepository,
    IRecommendationCommentRepository commentRepository)
    : IRecommendationDiscussionReadService {
    public async Task<Result<IReadOnlyList<RecommendationCommentModel>>> GetAsync(
        UserId userId,
        Guid recommendationId,
        CancellationToken cancellationToken) {
        Result<RecommendationId> recommendationIdResult = RequiredIdParser.Parse(
            recommendationId,
            nameof(recommendationId),
            "Recommendation id must not be empty.",
            value => new RecommendationId(value));
        if (recommendationIdResult.IsFailure) {
            return Result.Failure<IReadOnlyList<RecommendationCommentModel>>(recommendationIdResult.Error);
        }

        Recommendation? recommendation = await recommendationRepository.GetByIdAsync(
            recommendationIdResult.Value,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (recommendation is null ||
            (recommendation.ClientUserId != userId && recommendation.DietologistUserId != userId)) {
            return Result.Failure<IReadOnlyList<RecommendationCommentModel>>(Errors.Dietologist.InvitationNotFound);
        }

        IReadOnlyList<RecommendationCommentReadModel> comments =
            await commentRepository.GetByRecommendationAsync(recommendation.Id, cancellationToken).ConfigureAwait(false);
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
