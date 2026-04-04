using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Commands.MarkRecommendationRead;

public class MarkRecommendationReadCommandHandler(
    IRecommendationRepository recommendationRepository)
    : ICommandHandler<MarkRecommendationReadCommand, Result> {
    public async Task<Result> Handle(MarkRecommendationReadCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);
        var recommendationId = new RecommendationId(command.RecommendationId);

        var recommendation = await recommendationRepository.GetByIdAsync(
            recommendationId, asTracking: true, cancellationToken: cancellationToken);

        if (recommendation is null || recommendation.ClientUserId != userId) {
            return Result.Failure(Errors.Dietologist.InvitationNotFound);
        }

        recommendation.MarkAsRead();
        await recommendationRepository.UpdateAsync(recommendation, cancellationToken);
        return Result.Success();
    }
}
