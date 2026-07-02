using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Entities.Dietologist;

namespace FoodDiary.Application.Dietologist.Commands.AcceptInvitationForCurrentUser;

public sealed class AcceptInvitationForCurrentUserCommandHandler(
    IDietologistInvitationRepository invitationRepository,
    IDietologistUserContextService dietologistUserContextService,
    IUserRepository userRepository,
    INotificationWriter notificationWriter,
    INotificationRepository notificationRepository,
    INotificationPusher notificationPusher,
    IPostCommitActionQueue postCommitActionQueue)
    : ICommandHandler<AcceptInvitationForCurrentUserCommand, Result> {
    public async Task<Result> Handle(AcceptInvitationForCurrentUserCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure(Errors.Authentication.InvalidToken);
        }

        var dietologistUserId = new UserId(command.UserId!.Value);
        Result<User> userResult = await dietologistUserContextService.GetAccessibleUserAsync(dietologistUserId, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure(userResult.Error);
        }

        User user = userResult.Value;
        DietologistInvitation? invitation = await invitationRepository.GetByIdAsync(
            new DietologistInvitationId(command.InvitationId),
            asTracking: true,
            cancellationToken).ConfigureAwait(false);
        if (invitation is null || invitation.Status != DietologistInvitationStatus.Pending) {
            return Result.Failure(Errors.Dietologist.InvitationNotFound);
        }

        if (!string.Equals(invitation.DietologistEmail, user.Email, StringComparison.OrdinalIgnoreCase)) {
            return Result.Failure(Errors.Dietologist.AccessDenied);
        }

        if (invitation.IsExpired()) {
            return Result.Failure(Errors.Dietologist.InvitationExpired);
        }

        invitation.Accept(dietologistUserId);

        if (!user.HasRole(RoleNames.Dietologist)) {
            var roles = user.GetRoleNames().ToList();
            roles.Add(RoleNames.Dietologist);
            IReadOnlyList<Role> roleEntities = await userRepository.GetRolesByNamesAsync(roles, cancellationToken).ConfigureAwait(false);
            user.ReplaceRoles(roleEntities);
            await userRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        }

        await invitationRepository.UpdateAsync(invitation, cancellationToken).ConfigureAwait(false);
        await DietologistInvitationClientNotifier.NotifyAcceptedAsync(
            notificationWriter,
            notificationRepository,
            notificationPusher,
            postCommitActionQueue,
            invitation.ClientUserId,
            ResolveDietologistDisplayName(user),
            invitation.Id.Value.ToString(),
            cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    private static string ResolveDietologistDisplayName(User user) {
        string fullName = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? user.Email : fullName;
    }
}
