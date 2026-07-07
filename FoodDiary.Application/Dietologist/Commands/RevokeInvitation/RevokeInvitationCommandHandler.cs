using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Dietologist;

namespace FoodDiary.Application.Dietologist.Commands.RevokeInvitation;

public sealed class RevokeInvitationCommandHandler(
    IDietologistInvitationWriteRepository invitationRepository,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<RevokeInvitationCommand, Result> {
    public async Task<Result> Handle(RevokeInvitationCommand command, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver
            .ResolveAsync(command.UserId, currentUserAccessService, cancellationToken)
            .ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return Result.Failure(userIdResult.Error);
        }

        UserId userId = userIdResult.Value;
        DietologistInvitation? pending = await invitationRepository.GetByClientAndStatusAsync(
            userId, DietologistInvitationStatus.Pending, asTracking: true, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (pending is not null) {
            pending.Revoke();
            await invitationRepository.UpdateAsync(pending, cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }

        DietologistInvitation? active = await invitationRepository.GetActiveByClientAsync(userId, asTracking: true, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (active is null) {
            return Result.Failure(Errors.Dietologist.NoActiveRelationship);
        }

        active.Revoke();
        await invitationRepository.UpdateAsync(active, cancellationToken).ConfigureAwait(false);
        return Result.Success();

    }
}
