using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Commands.DisconnectDietologist;

public class DisconnectDietologistCommandHandler(
    IDietologistInvitationRepository invitationRepository,
    IUserRepository userRepository)
    : ICommandHandler<DisconnectDietologistCommand, Result> {
    public async Task<Result> Handle(DisconnectDietologistCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure(Errors.Authentication.InvalidToken);
        }

        var dietologistUserId = new UserId(command.UserId!.Value);
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, dietologistUserId, cancellationToken);
        if (accessError is not null) {
            return Result.Failure(accessError);
        }

        var clientUserId = new UserId(command.ClientUserId);
        var invitation = await invitationRepository.GetActiveByClientAndDietologistAsync(
            clientUserId, dietologistUserId, cancellationToken);

        if (invitation is null) {
            return Result.Failure(Errors.Dietologist.NoActiveRelationship);
        }

        invitation.Revoke();
        await invitationRepository.UpdateAsync(invitation, cancellationToken);
        return Result.Success();
    }
}
