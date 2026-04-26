using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Authentication.Common;

namespace FoodDiary.Application.Dietologist.Commands.DeclineInvitation;

public class DeclineInvitationCommandHandler(
    IDietologistInvitationRepository invitationRepository,
    IPasswordHasher passwordHasher,
    INotificationRepository notificationRepository,
    INotificationPusher notificationPusher,
    IWebPushNotificationSender webPushNotificationSender)
    : ICommandHandler<DeclineInvitationCommand, Result> {
    public async Task<Result> Handle(DeclineInvitationCommand command, CancellationToken cancellationToken) {
        var invitationId = new DietologistInvitationId(command.InvitationId);
        var invitation = await invitationRepository.GetByIdAsync(invitationId, asTracking: true, cancellationToken);

        if (invitation is null || invitation.Status != DietologistInvitationStatus.Pending) {
            return Result.Failure(Errors.Dietologist.InvitationNotFound);
        }

        if (!passwordHasher.Verify(command.Token, invitation.TokenHash)) {
            return Result.Failure(Errors.Dietologist.InvitationInvalidToken);
        }

        invitation.Decline();
        await invitationRepository.UpdateAsync(invitation, cancellationToken);
        await DietologistInvitationClientNotifier.NotifyDeclinedAsync(
            notificationRepository,
            notificationPusher,
            webPushNotificationSender,
            invitation.ClientUserId,
            invitation.DietologistEmail,
            invitation.Id.Value.ToString(),
            cancellationToken);
        return Result.Success();
    }
}
