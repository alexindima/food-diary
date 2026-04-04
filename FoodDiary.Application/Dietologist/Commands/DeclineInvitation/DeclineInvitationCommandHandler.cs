using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Commands.DeclineInvitation;

public class DeclineInvitationCommandHandler(
    IDietologistInvitationRepository invitationRepository,
    IPasswordHasher passwordHasher)
    : ICommandHandler<DeclineInvitationCommand, Result> {
    public async Task<Result> Handle(DeclineInvitationCommand command, CancellationToken cancellationToken) {
        var invitationId = new DietologistInvitationId(command.InvitationId);
        var invitation = await invitationRepository.GetByIdAsync(invitationId, cancellationToken);

        if (invitation is null || invitation.Status != DietologistInvitationStatus.Pending) {
            return Result.Failure(Errors.Dietologist.InvitationNotFound);
        }

        if (!passwordHasher.Verify(command.Token, invitation.TokenHash)) {
            return Result.Failure(Errors.Dietologist.InvitationInvalidToken);
        }

        invitation.Decline();
        await invitationRepository.UpdateAsync(invitation, cancellationToken);
        return Result.Success();
    }
}
