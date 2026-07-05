using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Services;

public sealed class DietologistRecommendationReadService(
    IDietologistInvitationReadRepository invitationRepository,
    IRecommendationReadRepository recommendationRepository,
    ICurrentUserAccessService currentUserAccessService)
    : IDietologistRecommendationReadService {
    public async Task<Result<IReadOnlyList<RecommendationModel>>> GetForCurrentUserAsync(
        UserId userId,
        CancellationToken cancellationToken) {
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<IReadOnlyList<RecommendationModel>>(accessError);
        }

        IReadOnlyList<RecommendationReadModel> recommendations = await recommendationRepository.GetByClientReadModelsAsync(userId, cancellationToken: cancellationToken).ConfigureAwait(false);
        var models = recommendations.Select(ToModel).ToList();
        return Result.Success<IReadOnlyList<RecommendationModel>>(models);
    }

    public async Task<Result<IReadOnlyList<RecommendationModel>>> GetForClientAsync(
        UserId dietologistUserId,
        Guid clientUserId,
        CancellationToken cancellationToken) {
        Error? currentUserAccessError = await currentUserAccessService.EnsureCanAccessAsync(dietologistUserId, cancellationToken).ConfigureAwait(false);
        if (currentUserAccessError is not null) {
            return Result.Failure<IReadOnlyList<RecommendationModel>>(currentUserAccessError);
        }

        var client = new UserId(clientUserId);
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
