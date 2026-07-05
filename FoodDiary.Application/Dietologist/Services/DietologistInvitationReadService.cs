using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Mappings;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Services;

public sealed class DietologistInvitationReadService(
    IDietologistInvitationReadRepository invitationRepository,
    IDietologistUserContextService dietologistUserContextService,
    ICurrentUserAccessService currentUserAccessService)
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

        DietologistInvitation? invitation = await invitationRepository.GetByIdAsync(
            new DietologistInvitationId(invitationId),
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (invitation is null) {
            return Result.Failure<DietologistInvitationForCurrentUserModel>(Errors.Dietologist.InvitationNotFound);
        }

        if (!string.Equals(invitation.DietologistEmail, userEmailResult.Value, StringComparison.OrdinalIgnoreCase)) {
            return Result.Failure<DietologistInvitationForCurrentUserModel>(Errors.Dietologist.AccessDenied);
        }

        return Result.Success(invitation.ToCurrentUserInvitationModel());
    }

    public async Task<Result<InvitationModel>> GetByTokenAsync(
        UserId userId,
        Guid invitationId,
        CancellationToken cancellationToken) {
        var typedInvitationId = new DietologistInvitationId(invitationId);
        DietologistInvitation? invitation = await invitationRepository.GetByIdAsync(typedInvitationId, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (invitation is null || invitation.Status != DietologistInvitationStatus.Pending) {
            return Result.Failure<InvitationModel>(Errors.Dietologist.InvitationNotFound);
        }

        string? userEmail = await dietologistUserContextService.GetUserEmailByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userEmail is null ||
            !string.Equals(invitation.DietologistEmail, userEmail, StringComparison.OrdinalIgnoreCase)) {
            return Result.Failure<InvitationModel>(Errors.Dietologist.InvitationNotFound);
        }

        if (invitation.IsExpired()) {
            return Result.Failure<InvitationModel>(Errors.Dietologist.InvitationExpired);
        }

        return Result.Success(invitation.ToInvitationModel());
    }

    public async Task<Result<DietologistInfoModel?>> GetMyDietologistAsync(
        UserId userId,
        CancellationToken cancellationToken) {
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<DietologistInfoModel?>(accessError);
        }

        DietologistInvitation? invitation = await invitationRepository.GetActiveByClientAsync(userId, cancellationToken: cancellationToken).ConfigureAwait(false);
        return Result.Success(invitation?.ToDietologistInfoModel());
    }

    public async Task<Result<IReadOnlyList<ClientSummaryModel>>> GetMyClientsAsync(
        UserId userId,
        CancellationToken cancellationToken) {
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<IReadOnlyList<ClientSummaryModel>>(accessError);
        }

        IReadOnlyList<DietologistInvitation> invitations = await invitationRepository.GetActiveByDietologistAsync(userId, cancellationToken).ConfigureAwait(false);
        var clients = invitations.Select(static invitation => invitation.ToClientSummaryModel()).ToList();
        return Result.Success<IReadOnlyList<ClientSummaryModel>>(clients);
    }

    public async Task<Result<DietologistRelationshipModel?>> GetMyRelationshipAsync(
        UserId userId,
        CancellationToken cancellationToken) {
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<DietologistRelationshipModel?>(accessError);
        }

        DietologistInvitation? accepted = await invitationRepository.GetActiveByClientAsync(userId, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (accepted is not null) {
            return Result.Success<DietologistRelationshipModel?>(accepted.ToRelationshipModel());
        }

        DietologistInvitation? pending = await invitationRepository.GetByClientAndStatusAsync(
            userId,
            DietologistInvitationStatus.Pending,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return Result.Success(pending?.ToRelationshipModel());
    }
}
