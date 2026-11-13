using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Common;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Models;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Dietologist.Application.Common;
using FoodDiary.Modules.Dietologist.Application.Models;
using FoodDiary.Modules.Dietologist.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Dietologist.Application.Commands.BulkCreateRecommendations;

public sealed class BulkCreateRecommendationsCommandHandler(
    IRecommendationWriteRepository recommendationRepository,
    IRecommendationBulkDispatchLookupRepository dispatchLookupRepository,
    IRecommendationBulkDispatchWriteRepository dispatchWriteRepository,
    IDietologistInvitationReadModelRepository invitationRepository,
    ICurrentUserAccessService userContextService)
    : ICommandHandler<BulkCreateRecommendationsCommand, Result<BulkRecommendationResultModel>> {
    public async Task<Result<BulkRecommendationResultModel>> Handle(
        BulkCreateRecommendationsCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> dietologistIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId, userContextService, cancellationToken);
        if (dietologistIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<BulkRecommendationResultModel>(dietologistIdResult);
        }

        UserId dietologistId = dietologistIdResult.Value;
        UserId[] clientIds = [.. command.ClientUserIds.Select(id => new UserId(id))];
        string idempotencyKey = command.IdempotencyKey.Trim();
        IReadOnlyList<RecommendationBulkDispatchReadModel> existing = await dispatchLookupRepository.GetExistingAsync(
            dietologistId,
            idempotencyKey,
            clientIds,
            cancellationToken);
        var existingByClient = existing.ToDictionary(
            dispatch => dispatch.ClientUserId,
            dispatch => dispatch.RecommendationId);

        List<BulkRecommendationRecipientResultModel> results = [with(capacity: clientIds.Length)];
        foreach (UserId clientId in clientIds) {
            BulkRecommendationRecipientResultModel result = await ProcessClientAsync(
                dietologistId,
                clientId,
                command.Text,
                idempotencyKey,
                existingByClient,
                cancellationToken);
            results.Add(result);
        }

        return Result.Success(new BulkRecommendationResultModel(idempotencyKey, results));
    }

    private async Task<BulkRecommendationRecipientResultModel> ProcessClientAsync(
        UserId dietologistId,
        UserId clientId,
        string text,
        string idempotencyKey,
        IReadOnlyDictionary<Guid, Guid> existingByClient,
        CancellationToken cancellationToken) {
        if (existingByClient.TryGetValue(clientId.Value, out Guid recommendationId)) {
            return new BulkRecommendationRecipientResultModel.AlreadyProcessed(clientId.Value, recommendationId);
        }

        Result<DietologistPermissionsModel> accessResult = await DietologistAccessPolicy.EnsureCanAccessClientReadModelAsync(
            invitationRepository,
            dietologistId,
            clientId,
            cancellationToken);
        if (accessResult.IsFailure) {
            return new BulkRecommendationRecipientResultModel.Rejected(clientId.Value, accessResult.Error.Code);
        }

        Error? permissionError = DietologistAccessPolicy.EnsureRecommendationPermission(accessResult.Value);
        if (permissionError is not null) {
            return new BulkRecommendationRecipientResultModel.Rejected(clientId.Value, permissionError.Code);
        }

        var recommendation = Recommendation.Create(dietologistId, clientId, text);
        await recommendationRepository.AddAsync(recommendation, cancellationToken);
        await dispatchWriteRepository.AddAsync(
            RecommendationBulkDispatch.Create(dietologistId, clientId, recommendation.Id, idempotencyKey),
            cancellationToken);
        return new BulkRecommendationRecipientResultModel.Created(clientId.Value, recommendation.Id.Value);
    }
}
