using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Mappings;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

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
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken);
        if (accessError is not null) {
            return Result.Failure(accessError);
        }

        var invitation = await invitationRepository.GetByClientAndStatusAsync(
            userId,
            DietologistInvitationStatus.Pending,
            asTracking: true,
            cancellationToken: cancellationToken);

        invitation ??= await invitationRepository.GetActiveByClientAsync(
            userId,
            asTracking: true,
            cancellationToken: cancellationToken);

        if (invitation is null) {
            return Result.Failure(Errors.Dietologist.NoActiveRelationship);
        }

        invitation.UpdatePermissions(command.Permissions.ToPermissions());
        await invitationRepository.UpdateAsync(invitation, cancellationToken);
        return Result.Success();
    }
}
