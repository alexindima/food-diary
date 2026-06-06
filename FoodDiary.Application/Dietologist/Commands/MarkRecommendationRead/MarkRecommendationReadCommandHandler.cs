using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Domain.Entities.Dietologist;

namespace FoodDiary.Application.Dietologist.Commands.MarkRecommendationRead;

public class MarkRecommendationReadCommandHandler(
    IRecommendationRepository recommendationRepository,
    IUserRepository userRepository)
    : ICommandHandler<MarkRecommendationReadCommand, Result> {
    public async Task<Result> Handle(MarkRecommendationReadCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);
        Error? currentUserAccessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(
            userRepository, userId, cancellationToken).ConfigureAwait(false);
        if (currentUserAccessError is not null) {
            return Result.Failure(currentUserAccessError);
        }

        var recommendationId = new RecommendationId(command.RecommendationId);

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
