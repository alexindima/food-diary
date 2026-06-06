using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Dietologist;

namespace FoodDiary.Application.Dietologist.Commands.RevokeInvitation;

public class RevokeInvitationCommandHandler(
    IDietologistInvitationRepository invitationRepository,
    IUserRepository userRepository)
    : ICommandHandler<RevokeInvitationCommand, Result> {
    public async Task<Result> Handle(RevokeInvitationCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);
        Error? accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure(accessError);
        }

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
