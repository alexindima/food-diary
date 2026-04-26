using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Mappings;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Dietologist.Common;

namespace FoodDiary.Application.Dietologist.Commands.CreateRecommendation;

public class CreateRecommendationCommandHandler(
    IDietologistInvitationRepository invitationRepository,
    IRecommendationRepository recommendationRepository)
    : ICommandHandler<CreateRecommendationCommand, Result<RecommendationModel>> {
    public async Task<Result<RecommendationModel>> Handle(
        CreateRecommendationCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<RecommendationModel>(Errors.Authentication.InvalidToken);
        }

        var dietologistUserId = new UserId(command.UserId!.Value);
        var clientUserId = new UserId(command.ClientUserId);

        var accessResult = await DietologistAccessPolicy.EnsureCanAccessClientAsync(
            invitationRepository, dietologistUserId, clientUserId, cancellationToken);

        if (accessResult.IsFailure) {
            return Result.Failure<RecommendationModel>(accessResult.Error);
        }

        var recommendation = Recommendation.Create(dietologistUserId, clientUserId, command.Text);
        await recommendationRepository.AddAsync(recommendation, cancellationToken);

        return Result.Success(recommendation.ToModel());
    }
}
