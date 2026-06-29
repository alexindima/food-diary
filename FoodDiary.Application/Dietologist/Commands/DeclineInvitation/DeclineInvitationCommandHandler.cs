using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Domain.Entities.Dietologist;

namespace FoodDiary.Application.Dietologist.Commands.DeclineInvitation;

public class DeclineInvitationCommandHandler(
    IDietologistInvitationRepository invitationRepository,
    IPasswordHasher passwordHasher,
    INotificationWriter notificationWriter,
    INotificationRepository notificationRepository,
    INotificationPusher notificationPusher,
    IPostCommitActionQueue postCommitActionQueue)
    : ICommandHandler<DeclineInvitationCommand, Result> {
    public async Task<Result> Handle(DeclineInvitationCommand command, CancellationToken cancellationToken) {
        var invitationId = new DietologistInvitationId(command.InvitationId);
        DietologistInvitation? invitation = await invitationRepository.GetByIdAsync(invitationId, asTracking: true, cancellationToken).ConfigureAwait(false);

        if (invitation is null || invitation.Status != DietologistInvitationStatus.Pending) {
            return Result.Failure(Errors.Dietologist.InvitationNotFound);
        }

        if (!passwordHasher.Verify(command.Token, invitation.TokenHash)) {
            return Result.Failure(Errors.Dietologist.InvitationInvalidToken);
        }

        invitation.Decline();
        await invitationRepository.UpdateAsync(invitation, cancellationToken).ConfigureAwait(false);
        await DietologistInvitationClientNotifier.NotifyDeclinedAsync(
            notificationWriter,
            notificationRepository,
            notificationPusher,
            postCommitActionQueue,
            invitation.ClientUserId,
            invitation.DietologistEmail,
            invitation.Id.Value.ToString(),
            cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
