using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Mappings;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Dietologist.Common;

namespace FoodDiary.Application.Dietologist.Queries.GetRecommendationsForClient;

public class GetRecommendationsForClientQueryHandler(
    IDietologistInvitationRepository invitationRepository,
    IRecommendationRepository recommendationRepository)
    : IQueryHandler<GetRecommendationsForClientQuery, Result<IReadOnlyList<RecommendationModel>>> {
    public async Task<Result<IReadOnlyList<RecommendationModel>>> Handle(
        GetRecommendationsForClientQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<IReadOnlyList<RecommendationModel>>(Errors.Authentication.InvalidToken);
        }

        var dietologistUserId = new UserId(query.UserId!.Value);
        var clientUserId = new UserId(query.ClientUserId);

        var accessResult = await DietologistAccessPolicy.EnsureCanAccessClientAsync(
            invitationRepository, dietologistUserId, clientUserId, cancellationToken);

        if (accessResult.IsFailure) {
            return Result.Failure<IReadOnlyList<RecommendationModel>>(accessResult.Error);
        }

        var recommendations = await recommendationRepository.GetByDietologistAndClientAsync(
            dietologistUserId, clientUserId, cancellationToken: cancellationToken);
        var models = recommendations.Select(r => r.ToModel()).ToList();
        return Result.Success<IReadOnlyList<RecommendationModel>>(models);
    }
}
