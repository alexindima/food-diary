using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Dietologist;

namespace FoodDiary.Application.Dietologist.Commands.MarkRecommendationRead;

public sealed class MarkRecommendationReadCommandHandler(
    IRecommendationWriteRepository recommendationRepository,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<MarkRecommendationReadCommand, Result> {
    public async Task<Result> Handle(MarkRecommendationReadCommand command, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver
            .ResolveAsync(command.UserId, currentUserAccessService, cancellationToken)
            .ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure(userIdResult);
        }

        UserId userId = userIdResult.Value;
        Result<RecommendationId> recommendationIdResult = RequiredIdParser.Parse(
            command.RecommendationId,
            nameof(command.RecommendationId),
            "Recommendation id must not be empty.",
            value => new RecommendationId(value));
        if (recommendationIdResult.IsFailure) {
            return RequiredIdParser.ToFailure(recommendationIdResult);
        }

        RecommendationId recommendationId = recommendationIdResult.Value;

        Recommendation? recommendation = await recommendationRepository.GetByIdAsync(
            recommendationId, asTracking: true, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (recommendation is null || recommendation.ClientUserId != userId) {
            return Result.Failure(Errors.Dietologist.InvitationNotFound);
        }

        recommendation.MarkAsRead();
        await recommendationRepository.UpdateAsync(recommendation, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
