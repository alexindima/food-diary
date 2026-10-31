using FoodDiary.Modules.Users.Contracts.Common.Validation;
using FoodDiary.Modules.Dietologist.Domain.Enums;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Dietologist.Domain.Entities;

namespace FoodDiary.Modules.Dietologist.Application.Commands.RevokeInvitation;

public sealed class RevokeInvitationCommandHandler(
    IDietologistInvitationWriteRepository invitationRepository,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<RevokeInvitationCommand, Result> {
    public async Task<Result> Handle(RevokeInvitationCommand command, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver
            .ResolveAsync(command.UserId, currentUserAccessService, cancellationToken);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure(userIdResult);
        }

        UserId userId = userIdResult.Value;
        DietologistInvitation? pending = await invitationRepository.GetByClientAndStatusAsync(
            userId, DietologistInvitationStatus.Pending, asTracking: true, cancellationToken: cancellationToken);

        if (pending is not null) {
            pending.Revoke();
            await invitationRepository.UpdateAsync(pending, cancellationToken);
            return Result.Success();
        }

        DietologistInvitation? active = await invitationRepository.GetActiveByClientAsync(userId, asTracking: true, cancellationToken: cancellationToken);
        if (active is null) {
            return Result.Failure(DietologistErrors.NoActiveRelationship);
        }

        active.Revoke();
        await invitationRepository.UpdateAsync(active, cancellationToken);
        return Result.Success();

    }
}
