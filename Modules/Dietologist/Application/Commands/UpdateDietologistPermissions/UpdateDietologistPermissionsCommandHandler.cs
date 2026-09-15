using FoodDiary.Modules.Dietologist.Domain.Enums;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Modules.Dietologist.Application.Mappings;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Dietologist.Domain.Entities;

namespace FoodDiary.Modules.Dietologist.Application.Commands.UpdateDietologistPermissions;

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
            return Result.Failure(DietologistErrors.NoActiveRelationship);
        }

        invitation.UpdatePermissions(command.Permissions.ToPermissions());
        await invitationRepository.UpdateAsync(invitation, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
