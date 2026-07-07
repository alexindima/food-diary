using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Dietologist;

namespace FoodDiary.Application.Dietologist.Commands.DisconnectDietologist;

public sealed class DisconnectDietologistCommandHandler(
    IDietologistInvitationWriteRepository invitationRepository,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<DisconnectDietologistCommand, Result> {
    public async Task<Result> Handle(DisconnectDietologistCommand command, CancellationToken cancellationToken) {
        Result<UserId> dietologistUserIdResult = await CurrentUserAccessResolver
            .ResolveAsync(command.UserId, currentUserAccessService, cancellationToken)
            .ConfigureAwait(false);
        if (dietologistUserIdResult.IsFailure) {
            return Result.Failure(dietologistUserIdResult.Error);
        }

        UserId dietologistUserId = dietologistUserIdResult.Value;
        var clientUserId = new UserId(command.ClientUserId);
        DietologistInvitation? invitation = await invitationRepository.GetActiveByClientAndDietologistAsync(
            clientUserId, dietologistUserId, cancellationToken).ConfigureAwait(false);

        if (invitation is null) {
            return Result.Failure(Errors.Dietologist.NoActiveRelationship);
        }

        invitation.Revoke();
        await invitationRepository.UpdateAsync(invitation, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
