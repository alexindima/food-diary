using FoodDiary.Application.Dietologist.Mappings;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Services;

public sealed class DietologistInvitationReadService(
    IDietologistInvitationReadModelRepository invitationRepository,
    IDietologistUserContextService dietologistUserContextService,
    ICurrentUserAccessService currentUserAccessService,
    TimeProvider timeProvider)
    : IDietologistInvitationReadService, IProfileDietologistReadService {
    public async Task<Result<DietologistInvitationForCurrentUserModel>> GetForCurrentUserAsync(
        UserId userId,
        Guid invitationId,
        CancellationToken cancellationToken) {
        Result<string> userEmailResult = await dietologistUserContextService
            .GetAccessibleUserEmailAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        if (userEmailResult.IsFailure) {
            return Result.Failure<DietologistInvitationForCurrentUserModel>(userEmailResult.Error);
        }

        Result<DietologistInvitationId> invitationIdResult = ParseInvitationId(invitationId);
        if (invitationIdResult.IsFailure) {
            return DietologistRequiredIdParser.ToFailure<DietologistInvitationForCurrentUserModel, DietologistInvitationId>(invitationIdResult);
        }

        DietologistInvitationReadModel? invitation = await invitationRepository.GetByIdReadModelAsync(
            invitationIdResult.Value,
            cancellationToken).ConfigureAwait(false);
        if (invitation is null) {
            return Result.Failure<DietologistInvitationForCurrentUserModel>(DietologistErrors.InvitationNotFound);
        }

        if (!string.Equals(invitation.DietologistEmail, userEmailResult.Value, StringComparison.OrdinalIgnoreCase)) {
            return Result.Failure<DietologistInvitationForCurrentUserModel>(DietologistErrors.AccessDenied);
        }

        return Result.Success(invitation.ToCurrentUserInvitationModel(timeProvider));
    }

    public async Task<Result<InvitationModel>> GetByTokenAsync(
        UserId userId,
        Guid invitationId,
        CancellationToken cancellationToken) {
        Result<DietologistInvitationId> invitationIdResult = ParseInvitationId(invitationId);
        if (invitationIdResult.IsFailure) {
            return DietologistRequiredIdParser.ToFailure<InvitationModel, DietologistInvitationId>(invitationIdResult);
        }

        DietologistInvitationReadModel? invitation = await invitationRepository.GetByIdReadModelAsync(invitationIdResult.Value, cancellationToken).ConfigureAwait(false);

        if (invitation is null || invitation.Status != DietologistInvitationStatus.Pending) {
            return Result.Failure<InvitationModel>(DietologistErrors.InvitationNotFound);
        }

        string? userEmail = await dietologistUserContextService.GetUserEmailByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userEmail is null ||
            !string.Equals(invitation.DietologistEmail, userEmail, StringComparison.OrdinalIgnoreCase)) {
            return Result.Failure<InvitationModel>(DietologistErrors.InvitationNotFound);
        }

        if (IsExpired(invitation)) {
            return Result.Failure<InvitationModel>(DietologistErrors.InvitationExpired);
        }

        return Result.Success(invitation.ToInvitationModel());
    }

    public async Task<Result<DietologistInfoModel?>> GetMyDietologistAsync(
        UserId userId,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            userId, currentUserAccessService, cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<DietologistInfoModel?>(userIdResult);
        }

        DietologistInvitationReadModel? invitation = await invitationRepository.GetActiveByClientReadModelAsync(userId, cancellationToken).ConfigureAwait(false);
        return Result.Success(invitation is null ? null : invitation.ToDietologistInfoModel());
    }

    public async Task<Result<IReadOnlyList<ClientSummaryModel>>> GetMyClientsAsync(
        UserId userId,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            userId, currentUserAccessService, cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<IReadOnlyList<ClientSummaryModel>>(userIdResult);
        }

        IReadOnlyList<DietologistInvitationReadModel> invitations = await invitationRepository.GetActiveByDietologistReadModelsAsync(userId, cancellationToken).ConfigureAwait(false);
        var clients = invitations.Select(invitation => invitation.ToClientSummaryModel()).ToList();
        return Result.Success<IReadOnlyList<ClientSummaryModel>>(clients);
    }

    public async Task<Result<DietologistRelationshipModel?>> GetMyRelationshipAsync(
        UserId userId,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            userId, currentUserAccessService, cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<DietologistRelationshipModel?>(userIdResult);
        }

        DietologistInvitationReadModel? accepted = await invitationRepository.GetActiveByClientReadModelAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accepted is not null) {
            return Result.Success<DietologistRelationshipModel?>(accepted.ToRelationshipModel());
        }

        DietologistInvitationReadModel? pending = await invitationRepository.GetByClientAndStatusReadModelAsync(
            userId,
            DietologistInvitationStatus.Pending,
            cancellationToken).ConfigureAwait(false);

        return Result.Success(pending is null ? null : pending.ToRelationshipModel());
    }

    async Task<Result<ProfileDietologistRelationshipModel?>> IProfileDietologistReadService.GetRelationshipAsync(
        UserId userId,
        CancellationToken cancellationToken) {
        Result<DietologistRelationshipModel?> result = await GetMyRelationshipAsync(userId, cancellationToken).ConfigureAwait(false);
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

    private static Result<DietologistInvitationId> ParseInvitationId(Guid invitationId) =>
        DietologistRequiredIdParser.Parse(
            invitationId,
            nameof(invitationId),
            "Invitation id must not be empty.",
            value => new DietologistInvitationId(value));

    private bool IsExpired(DietologistInvitationReadModel invitation) =>
        invitation.Status == DietologistInvitationStatus.Pending &&
        invitation.ExpiresAtUtc <= timeProvider.GetUtcNow().UtcDateTime;

}
