using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

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
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken);
        if (accessError is not null) {
            return Result.Failure(accessError);
        }

        var pending = await invitationRepository.GetByClientAndStatusAsync(
            userId, DietologistInvitationStatus.Pending, asTracking: true, cancellationToken: cancellationToken);

        if (pending is not null) {
            pending.Revoke();
            await invitationRepository.UpdateAsync(pending, cancellationToken);
            return Result.Success();
        }

        var active = await invitationRepository.GetActiveByClientAsync(userId, asTracking: true, cancellationToken: cancellationToken);
        if (active is not null) {
            active.Revoke();
            await invitationRepository.UpdateAsync(active, cancellationToken);
            return Result.Success();
        }

        return Result.Failure(Errors.Dietologist.NoActiveRelationship);
    }
}
