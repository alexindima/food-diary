using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Services;

public sealed class DietologistInvitationReadService(
    IDietologistInvitationReadRepository invitationRepository,
    IDietologistUserContextService dietologistUserContextService,
    ICurrentUserAccessService currentUserAccessService,
    TimeProvider timeProvider)
    : IDietologistInvitationReadService {
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

        DietologistInvitationReadModel? invitation = await invitationRepository.GetByIdReadModelAsync(
            new DietologistInvitationId(invitationId),
            cancellationToken).ConfigureAwait(false);
        if (invitation is null) {
            return Result.Failure<DietologistInvitationForCurrentUserModel>(Errors.Dietologist.InvitationNotFound);
        }

        if (!string.Equals(invitation.DietologistEmail, userEmailResult.Value, StringComparison.OrdinalIgnoreCase)) {
            return Result.Failure<DietologistInvitationForCurrentUserModel>(Errors.Dietologist.AccessDenied);
        }

        return Result.Success(ToCurrentUserInvitationModel(invitation, timeProvider));
    }

    public async Task<Result<InvitationModel>> GetByTokenAsync(
        UserId userId,
        Guid invitationId,
        CancellationToken cancellationToken) {
        var typedInvitationId = new DietologistInvitationId(invitationId);
        DietologistInvitationReadModel? invitation = await invitationRepository.GetByIdReadModelAsync(typedInvitationId, cancellationToken).ConfigureAwait(false);

        if (invitation is null || invitation.Status != DietologistInvitationStatus.Pending) {
            return Result.Failure<InvitationModel>(Errors.Dietologist.InvitationNotFound);
        }

        string? userEmail = await dietologistUserContextService.GetUserEmailByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userEmail is null ||
            !string.Equals(invitation.DietologistEmail, userEmail, StringComparison.OrdinalIgnoreCase)) {
            return Result.Failure<InvitationModel>(Errors.Dietologist.InvitationNotFound);
        }

        if (IsExpired(invitation)) {
            return Result.Failure<InvitationModel>(Errors.Dietologist.InvitationExpired);
        }

        return Result.Success(ToInvitationModel(invitation));
    }

    public async Task<Result<DietologistInfoModel?>> GetMyDietologistAsync(
        UserId userId,
        CancellationToken cancellationToken) {
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<DietologistInfoModel?>(accessError);
        }

        DietologistInvitationReadModel? invitation = await invitationRepository.GetActiveByClientReadModelAsync(userId, cancellationToken).ConfigureAwait(false);
        return Result.Success(invitation is null ? null : ToDietologistInfoModel(invitation));
    }

    public async Task<Result<IReadOnlyList<ClientSummaryModel>>> GetMyClientsAsync(
        UserId userId,
        CancellationToken cancellationToken) {
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<IReadOnlyList<ClientSummaryModel>>(accessError);
        }

        IReadOnlyList<DietologistInvitationReadModel> invitations = await invitationRepository.GetActiveByDietologistReadModelsAsync(userId, cancellationToken).ConfigureAwait(false);
        var clients = invitations.Select(ToClientSummaryModel).ToList();
        return Result.Success<IReadOnlyList<ClientSummaryModel>>(clients);
    }

    public async Task<Result<DietologistRelationshipModel?>> GetMyRelationshipAsync(
        UserId userId,
        CancellationToken cancellationToken) {
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<DietologistRelationshipModel?>(accessError);
        }

        DietologistInvitationReadModel? accepted = await invitationRepository.GetActiveByClientReadModelAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accepted is not null) {
            return Result.Success<DietologistRelationshipModel?>(ToRelationshipModel(accepted));
        }

        DietologistInvitationReadModel? pending = await invitationRepository.GetByClientAndStatusReadModelAsync(
            userId,
            DietologistInvitationStatus.Pending,
            cancellationToken).ConfigureAwait(false);

        return Result.Success(pending is null ? null : ToRelationshipModel(pending));
    }

    private static DietologistRelationshipModel ToRelationshipModel(DietologistInvitationReadModel invitation) =>
        new(
            invitation.InvitationId,
            invitation.Status.ToString(),
            invitation.DietologistUserEmail ?? invitation.DietologistEmail,
            invitation.DietologistFirstName,
            invitation.DietologistLastName,
            invitation.DietologistUserId,
            invitation.Permissions.ToApplicationModel(),
            invitation.CreatedAtUtc,
            invitation.ExpiresAtUtc,
            invitation.AcceptedAtUtc);

    private static DietologistInfoModel ToDietologistInfoModel(DietologistInvitationReadModel invitation) =>
        new(
            invitation.InvitationId,
            invitation.DietologistUserId!.Value,
            invitation.DietologistUserEmail!,
            invitation.DietologistFirstName,
            invitation.DietologistLastName,
            invitation.Permissions.ToApplicationModel(),
            invitation.AcceptedAtUtc!.Value);

    private static ClientSummaryModel ToClientSummaryModel(DietologistInvitationReadModel invitation) =>
        new(
            invitation.ClientUserId,
            invitation.ClientEmail,
            invitation.Permissions.ShareProfile ? invitation.ClientFirstName : null,
            invitation.Permissions.ShareProfile ? invitation.ClientLastName : null,
            invitation.Permissions.ShareProfile ? invitation.ClientProfileImage : null,
            invitation.Permissions.ShareProfile ? invitation.ClientBirthDate : null,
            invitation.Permissions.ShareProfile ? invitation.ClientGender : null,
            invitation.Permissions.ShareProfile ? invitation.ClientHeight : null,
            invitation.Permissions.ShareProfile ? invitation.ClientActivityLevel.ToString() : null,
            invitation.Permissions.ToApplicationModel(),
            invitation.AcceptedAtUtc!.Value);

    private static InvitationModel ToInvitationModel(DietologistInvitationReadModel invitation) =>
        new(
            invitation.InvitationId,
            invitation.ClientEmail,
            invitation.ClientFirstName,
            invitation.ClientLastName,
            invitation.Status.ToString(),
            invitation.CreatedAtUtc,
            invitation.ExpiresAtUtc);

    private bool IsExpired(DietologistInvitationReadModel invitation) =>
        invitation.Status == DietologistInvitationStatus.Pending &&
        invitation.ExpiresAtUtc < timeProvider.GetUtcNow().UtcDateTime;

    private static DietologistInvitationForCurrentUserModel ToCurrentUserInvitationModel(
        DietologistInvitationReadModel invitation,
        TimeProvider timeProvider) =>
        new(
            invitation.InvitationId,
            invitation.ClientUserId,
            invitation.ClientEmail,
            invitation.ClientFirstName,
            invitation.ClientLastName,
            invitation.Status == DietologistInvitationStatus.Pending &&
                invitation.ExpiresAtUtc < timeProvider.GetUtcNow().UtcDateTime
                    ? "Expired"
                    : invitation.Status.ToString(),
            invitation.CreatedAtUtc,
            invitation.ExpiresAtUtc);
}
