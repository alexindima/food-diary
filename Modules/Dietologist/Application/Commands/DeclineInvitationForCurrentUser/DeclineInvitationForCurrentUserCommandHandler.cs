using FoodDiary.Modules.Dietologist.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Dietologist.Domain.Enums;
using FoodDiary.Modules.Dietologist.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Results;
using FoodDiary.Modules.Dietologist.Application.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Common;
using FoodDiary.Modules.Notifications.Contracts.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Modules.Dietologist.Domain.Entities;

namespace FoodDiary.Modules.Dietologist.Application.Commands.DeclineInvitationForCurrentUser;

public sealed class DeclineInvitationForCurrentUserCommandHandler(
    IDietologistInvitationWriteRepository invitationRepository,
    IDietologistUserContextService dietologistUserContextService,
    INotificationWriter notificationWriter,
    INotificationClientRefreshService notificationClientRefreshService,
    IPostCommitActionQueue postCommitActionQueue)
    : ICommandHandler<DeclineInvitationForCurrentUserCommand, Result> {
    public async Task<Result> Handle(DeclineInvitationForCurrentUserCommand command, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            dietologistUserContextService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return Result.Failure(userIdResult.Error);
        }

        UserId userId = userIdResult.Value;
        Result<UserDietologistProfileModel> userResult = await dietologistUserContextService.GetAccessibleProfileAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure(userResult.Error);
        }

        UserDietologistProfileModel user = userResult.Value;
        Result<DietologistInvitationId> invitationIdResult = DietologistRequiredIdParser.Parse(
            command.InvitationId,
            nameof(command.InvitationId),
            "Invitation id must not be empty.",
            value => new DietologistInvitationId(value));
        if (invitationIdResult.IsFailure) {
            return DietologistRequiredIdParser.ToFailure(invitationIdResult);
        }

        DietologistInvitation? invitation = await invitationRepository.GetByIdAsync(
            invitationIdResult.Value,
            asTracking: true,
            cancellationToken).ConfigureAwait(false);
        if (invitation is null || invitation.Status != DietologistInvitationStatus.Pending) {
            return Result.Failure(DietologistErrors.InvitationNotFound);
        }

        if (!string.Equals(invitation.DietologistEmail, user.Email, StringComparison.OrdinalIgnoreCase)) {
            return Result.Failure(DietologistErrors.AccessDenied);
        }

        if (invitation.IsExpired()) {
            return Result.Failure(DietologistErrors.InvitationExpired);
        }

        invitation.Decline();
        await invitationRepository.UpdateAsync(invitation, cancellationToken).ConfigureAwait(false);
        await DietologistInvitationClientNotifier.NotifyDeclinedAsync(
            notificationWriter,
            notificationClientRefreshService,
            postCommitActionQueue,
            invitation.ClientUserId,
            ResolveDietologistDisplayName(user),
            invitation.Id.Value.ToString(),
            cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    private static string ResolveDietologistDisplayName(UserDietologistProfileModel user) {
        return DietologistProfileDisplayName.Resolve(user);
    }
}
