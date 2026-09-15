using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Dietologist.Application.Mappings;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Common;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Models;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Modules.Dietologist.Application.Common;
using FoodDiary.Modules.Dietologist.Application.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Dietologist.Application.Queries.GetRecommendationsForClient;

public sealed class GetRecommendationsForClientQueryHandler(
    IDietologistInvitationReadModelRepository invitationRepository,
    IRecommendationReadModelRepository recommendationRepository,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetRecommendationsForClientQuery, Result<IReadOnlyList<RecommendationModel>>> {
    public async Task<Result<IReadOnlyList<RecommendationModel>>> Handle(
        GetRecommendationsForClientQuery query, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<IReadOnlyList<RecommendationModel>>(userIdResult);
        }

        UserId dietologistUserId = userIdResult.Value;
        Guid clientUserId = query.ClientUserId;
        Result<UserId> clientResult = UserIdParser.Parse(
            clientUserId,
            Errors.Validation.Invalid(nameof(clientUserId), "Client user id must not be empty."));
        if (clientResult.IsFailure) {
            return UserIdParser.ToFailure<IReadOnlyList<RecommendationModel>>(clientResult);
        }

        UserId client = clientResult.Value;
        Result<DietologistPermissionsModel> accessResult = await DietologistAccessPolicy.EnsureCanAccessClientReadModelAsync(
            invitationRepository, dietologistUserId, client, cancellationToken).ConfigureAwait(false);

        if (accessResult.IsFailure) {
            return Result.Failure<IReadOnlyList<RecommendationModel>>(accessResult.Error);
        }

        IReadOnlyList<RecommendationReadModel> recommendations = await recommendationRepository.GetByDietologistAndClientReadModelsAsync(
            dietologistUserId, client, cancellationToken: cancellationToken).ConfigureAwait(false);
        var models = recommendations.Select(recommendation => recommendation.ToModel()).ToList();
        return Result.Success<IReadOnlyList<RecommendationModel>>(models);

    }

}
