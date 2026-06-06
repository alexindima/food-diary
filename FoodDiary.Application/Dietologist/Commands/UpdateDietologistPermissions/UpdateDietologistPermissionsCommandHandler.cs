using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Dietologist.Mappings;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Dietologist;

namespace FoodDiary.Application.Dietologist.Commands.UpdateDietologistPermissions;

public class UpdateDietologistPermissionsCommandHandler(
    IDietologistInvitationRepository invitationRepository,
    IUserRepository userRepository)
    : ICommandHandler<UpdateDietologistPermissionsCommand, Result> {
    public async Task<Result> Handle(UpdateDietologistPermissionsCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);
        Error? accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure(accessError);
        }

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
