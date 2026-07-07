using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Common.Validation;
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
    IDietologistInvitationWriteRepository invitationRepository,
    IDietologistUserContextService dietologistUserContextService,
    IUserRoleMembershipService userRoleMembershipService,
    INotificationWriter notificationWriter,
    INotificationReadRepository notificationRepository,
    INotificationPusher notificationPusher,
    IPostCommitActionQueue postCommitActionQueue)
    : ICommandHandler<AcceptInvitationForCurrentUserCommand, Result> {
    public async Task<Result> Handle(AcceptInvitationForCurrentUserCommand command, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure(userIdResult);
        }

        UserId dietologistUserId = userIdResult.Value;
        Result<User> userResult = await dietologistUserContextService.GetAccessibleUserAsync(dietologistUserId, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure(userResult.Error);
        }

        User user = userResult.Value;
        Result<DietologistInvitationId> invitationIdResult = RequiredIdParser.Parse(
            command.InvitationId,
            nameof(command.InvitationId),
            "Invitation id must not be empty.",
            value => new DietologistInvitationId(value));
        if (invitationIdResult.IsFailure) {
            return RequiredIdParser.ToFailure(invitationIdResult);
        }

        DietologistInvitation? invitation = await invitationRepository.GetByIdAsync(
            invitationIdResult.Value,
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
            await userRoleMembershipService.EnsureRoleAsync(user.Id, RoleNames.Dietologist, cancellationToken).ConfigureAwait(false);
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
