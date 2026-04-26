using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Commands.AcceptInvitationForCurrentUser;

public sealed class AcceptInvitationForCurrentUserCommandHandler(
    IDietologistInvitationRepository invitationRepository,
    IUserRepository userRepository,
    INotificationRepository notificationRepository,
    INotificationPusher notificationPusher,
    IWebPushNotificationSender webPushNotificationSender)
    : ICommandHandler<AcceptInvitationForCurrentUserCommand, Result> {
    public async Task<Result> Handle(AcceptInvitationForCurrentUserCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure(Errors.Authentication.InvalidToken);
        }

        var dietologistUserId = new UserId(command.UserId!.Value);
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, dietologistUserId, cancellationToken);
        if (accessError is not null) {
            return Result.Failure(accessError);
        }

        var user = await userRepository.GetByIdAsync(dietologistUserId, cancellationToken);
        if (user is null) {
            return Result.Failure(Errors.Authentication.InvalidToken);
        }

        var invitation = await invitationRepository.GetByIdAsync(
            new DietologistInvitationId(command.InvitationId),
            asTracking: true,
            cancellationToken);
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
            var roleEntities = await userRepository.GetRolesByNamesAsync(roles, cancellationToken);
            user.ReplaceRoles(roleEntities);
            await userRepository.UpdateAsync(user, cancellationToken);
        }

        await invitationRepository.UpdateAsync(invitation, cancellationToken);
        await DietologistInvitationClientNotifier.NotifyAcceptedAsync(
            notificationRepository,
            notificationPusher,
            webPushNotificationSender,
            invitation.ClientUserId,
            ResolveDietologistDisplayName(user),
            invitation.Id.Value.ToString(),
            cancellationToken);
        return Result.Success();
    }

    private static string ResolveDietologistDisplayName(FoodDiary.Domain.Entities.Users.User user) {
        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? user.Email : fullName;
    }
}
