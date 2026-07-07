using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Services;

public sealed class DietologistRecommendationReadService(
    IDietologistInvitationReadModelRepository invitationRepository,
    IRecommendationReadModelRepository recommendationRepository,
    ICurrentUserAccessService currentUserAccessService)
    : IDietologistRecommendationReadService {
    public async Task<Result<IReadOnlyList<RecommendationModel>>> GetForCurrentUserAsync(
        UserId userId,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            userId, currentUserAccessService, cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<IReadOnlyList<RecommendationModel>>(userIdResult);
        }

        IReadOnlyList<RecommendationReadModel> recommendations = await recommendationRepository.GetByClientReadModelsAsync(userId, cancellationToken: cancellationToken).ConfigureAwait(false);
        var models = recommendations.Select(ToModel).ToList();
        return Result.Success<IReadOnlyList<RecommendationModel>>(models);
    }

    public async Task<Result<IReadOnlyList<RecommendationModel>>> GetForClientAsync(
        UserId dietologistUserId,
        Guid clientUserId,
        CancellationToken cancellationToken) {
        Result<UserId> dietologistUserIdResult = await CurrentUserAccessResolver.ResolveAsync(
            dietologistUserId, currentUserAccessService, cancellationToken).ConfigureAwait(false);
        if (dietologistUserIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<IReadOnlyList<RecommendationModel>>(dietologistUserIdResult);
        }

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
        var models = recommendations.Select(ToModel).ToList();
        return Result.Success<IReadOnlyList<RecommendationModel>>(models);
    }

    private static RecommendationModel ToModel(RecommendationReadModel recommendation) =>
        new(
            recommendation.RecommendationId,
            recommendation.DietologistUserId,
            recommendation.DietologistFirstName,
            recommendation.DietologistLastName,
            recommendation.Text,
            recommendation.IsRead,
            recommendation.CreatedAtUtc,
            recommendation.ReadAtUtc);
}
