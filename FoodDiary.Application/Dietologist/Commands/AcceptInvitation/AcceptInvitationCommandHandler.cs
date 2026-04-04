using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Commands.AcceptInvitation;

public class AcceptInvitationCommandHandler(
    IDietologistInvitationRepository invitationRepository,
    IUserRepository userRepository,
    IPasswordHasher passwordHasher)
    : ICommandHandler<AcceptInvitationCommand, Result> {
    public async Task<Result> Handle(AcceptInvitationCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure(Errors.Authentication.InvalidToken);
        }

        var dietologistUserId = new UserId(command.UserId!.Value);
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, dietologistUserId, cancellationToken);
        if (accessError is not null) {
            return Result.Failure(accessError);
        }

        var invitationId = new DietologistInvitationId(command.InvitationId);
        var invitation = await invitationRepository.GetByIdAsync(invitationId, cancellationToken);

        if (invitation is null) {
            return Result.Failure(Errors.Dietologist.InvitationNotFound);
        }

        if (invitation.Status != DietologistInvitationStatus.Pending) {
            return Result.Failure(Errors.Dietologist.InvitationNotFound);
        }

        if (invitation.IsExpired()) {
            return Result.Failure(Errors.Dietologist.InvitationExpired);
        }

        if (!passwordHasher.Verify(command.Token, invitation.TokenHash)) {
            return Result.Failure(Errors.Dietologist.InvitationInvalidToken);
        }

        invitation.Accept(dietologistUserId);

        var user = (await userRepository.GetByIdAsync(dietologistUserId, cancellationToken))!;
        if (!user.HasRole(RoleNames.Dietologist)) {
            var roles = user.GetRoleNames().ToList();
            roles.Add(RoleNames.Dietologist);
            var roleEntities = await userRepository.GetRolesByNamesAsync(roles, cancellationToken);
            user.ReplaceRoles(roleEntities);
            await userRepository.UpdateAsync(user, cancellationToken);
        }

        await invitationRepository.UpdateAsync(invitation, cancellationToken);
        return Result.Success();
    }
}
