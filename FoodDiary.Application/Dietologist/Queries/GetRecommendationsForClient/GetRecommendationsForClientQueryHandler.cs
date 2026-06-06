using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Mappings;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Domain.Entities.Dietologist;

namespace FoodDiary.Application.Dietologist.Queries.GetRecommendationsForClient;

public class GetRecommendationsForClientQueryHandler(
    IDietologistInvitationRepository invitationRepository,
    IRecommendationRepository recommendationRepository,
    IUserRepository userRepository)
    : IQueryHandler<GetRecommendationsForClientQuery, Result<IReadOnlyList<RecommendationModel>>> {
    public async Task<Result<IReadOnlyList<RecommendationModel>>> Handle(
        GetRecommendationsForClientQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<IReadOnlyList<RecommendationModel>>(Errors.Authentication.InvalidToken);
        }

        var dietologistUserId = new UserId(query.UserId!.Value);
        Error? currentUserAccessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(
            userRepository, dietologistUserId, cancellationToken).ConfigureAwait(false);
        if (currentUserAccessError is not null) {
            return Result.Failure<IReadOnlyList<RecommendationModel>>(currentUserAccessError);
        }

        var clientUserId = new UserId(query.ClientUserId);

        Result<DietologistPermissionsModel> accessResult = await DietologistAccessPolicy.EnsureCanAccessClientAsync(
            invitationRepository, dietologistUserId, clientUserId, cancellationToken).ConfigureAwait(false);

        if (accessResult.IsFailure) {
            return Result.Failure<IReadOnlyList<RecommendationModel>>(accessResult.Error);
        }

        IReadOnlyList<Recommendation> recommendations = await recommendationRepository.GetByDietologistAndClientAsync(
            dietologistUserId, clientUserId, cancellationToken: cancellationToken).ConfigureAwait(false);
        var models = recommendations.Select(r => r.ToModel()).ToList();
        return Result.Success<IReadOnlyList<RecommendationModel>>(models);
    }
}
