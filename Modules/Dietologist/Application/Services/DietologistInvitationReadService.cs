using FoodDiary.Modules.Dietologist.Domain.Enums;
using FoodDiary.Modules.Dietologist.Application.Mappings;
using FoodDiary.Results;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Common;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Models;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Dietologist.Application.Common;
using FoodDiary.Modules.Dietologist.Application.Models;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Dietologist.Application.Services;

public sealed class DietologistInvitationReadService(
    IDietologistInvitationReadModelRepository invitationRepository,
    ICurrentUserAccessService currentUserAccessService)
    : IDietologistInvitationReadService, IProfileDietologistReadService {

    public async Task<Result<IReadOnlyList<ClientSummaryModel>>> GetMyClientsAsync(
        UserId userId,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            userId, currentUserAccessService, cancellationToken);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<IReadOnlyList<ClientSummaryModel>>(userIdResult);
        }

        IReadOnlyList<DietologistInvitationReadModel> invitations = await invitationRepository.GetActiveByDietologistReadModelsAsync(userId, cancellationToken);
        var clients = invitations.Select(invitation => invitation.ToClientSummaryModel()).ToList();
        return Result.Success<IReadOnlyList<ClientSummaryModel>>(clients);
    }

    public async Task<Result<IReadOnlyList<ClientSummaryModel>>> GetMyClientsPageAsync(
        UserId userId,
        int page,
        int limit,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            userId, currentUserAccessService, cancellationToken);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<IReadOnlyList<ClientSummaryModel>>(userIdResult);
        }

        IReadOnlyList<DietologistInvitationReadModel> invitations =
            await invitationRepository.GetActiveByDietologistPageReadModelsAsync(userId, page, limit, cancellationToken);
        return Result.Success<IReadOnlyList<ClientSummaryModel>>(
            invitations.Select(invitation => invitation.ToClientSummaryModel()).ToList());
    }

    public async Task<Result<DietologistRelationshipModel?>> GetMyRelationshipAsync(
        UserId userId,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            userId, currentUserAccessService, cancellationToken);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<DietologistRelationshipModel?>(userIdResult);
        }

        DietologistInvitationReadModel? accepted = await invitationRepository.GetActiveByClientReadModelAsync(userId, cancellationToken);
        if (accepted is not null) {
            return Result.Success<DietologistRelationshipModel?>(accepted.ToRelationshipModel());
        }

        DietologistInvitationReadModel? pending = await invitationRepository.GetByClientAndStatusReadModelAsync(
            userId,
            DietologistInvitationStatus.Pending,
            cancellationToken);

        return Result.Success(pending is null ? null : pending.ToRelationshipModel());
    }

    async Task<Result<ProfileDietologistRelationshipModel?>> IProfileDietologistReadService.GetRelationshipAsync(
        UserId userId,
        CancellationToken cancellationToken) {
        Result<DietologistRelationshipModel?> result = await GetMyRelationshipAsync(userId, cancellationToken);
        if (result.IsFailure) {
            return Result.Failure<ProfileDietologistRelationshipModel?>(result.Error);
        }

        return Result.Success(result.Value is null ? null : ToProfileRelationshipModel(result.Value));
    }

    private static ProfileDietologistRelationshipModel ToProfileRelationshipModel(DietologistRelationshipModel relationship) =>
        new(
            relationship.InvitationId,
            relationship.Status,
            relationship.Email,
            relationship.FirstName,
            relationship.LastName,
            relationship.DietologistUserId,
            new ProfileDietologistPermissionsModel(
                relationship.Permissions.ShareMeals,
                relationship.Permissions.ShareStatistics,
                relationship.Permissions.ShareWeight,
                relationship.Permissions.ShareWaist,
                relationship.Permissions.ShareGoals,
                relationship.Permissions.ShareHydration,
                relationship.Permissions.ShareProfile,
                relationship.Permissions.ShareFasting),
            relationship.CreatedAtUtc,
            relationship.ExpiresAtUtc,
            relationship.AcceptedAtUtc);

}
