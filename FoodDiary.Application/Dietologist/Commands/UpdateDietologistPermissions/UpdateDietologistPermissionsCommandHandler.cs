using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Dietologist.Mappings;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Dietologist;

namespace FoodDiary.Application.Dietologist.Commands.UpdateDietologistPermissions;

public sealed class UpdateDietologistPermissionsCommandHandler(
    IDietologistInvitationWriteRepository invitationRepository,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<UpdateDietologistPermissionsCommand, Result> {
    public async Task<Result> Handle(UpdateDietologistPermissionsCommand command, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver
            .ResolveAsync(command.UserId, currentUserAccessService, cancellationToken)
            .ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure(userIdResult);
        }

        UserId userId = userIdResult.Value;
        DietologistInvitation? invitation = await invitationRepository.GetByClientAndStatusAsync(
            userId,
            DietologistInvitationStatus.Pending,
            asTracking: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        invitation ??= await invitationRepository.GetActiveByClientAsync(
            userId,
            asTracking: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (invitation is null) {
            return Result.Failure(Errors.Dietologist.NoActiveRelationship);
        }

        invitation.UpdatePermissions(command.Permissions.ToPermissions());
        await invitationRepository.UpdateAsync(invitation, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
