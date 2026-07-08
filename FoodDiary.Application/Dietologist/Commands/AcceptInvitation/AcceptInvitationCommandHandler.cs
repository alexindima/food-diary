using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Dietologist.Commands.AcceptInvitation;

public sealed class AcceptInvitationCommandHandler(
    IDietologistInvitationWriteRepository invitationRepository,
    IDietologistUserContextService dietologistUserContextService,
    IUserRoleMembershipService userRoleMembershipService,
    IPasswordHasher passwordHasher,
    INotificationWriter notificationWriter,
    INotificationReadRepository notificationRepository,
    INotificationPusher notificationPusher,
    IPostCommitActionQueue postCommitActionQueue)
    : ICommandHandler<AcceptInvitationCommand, Result> {
    public async Task<Result> Handle(AcceptInvitationCommand command, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure(userIdResult);
        }

        UserId dietologistUserId = userIdResult.Value;
        Result<User> userResult = await dietologistUserContextService.GetAccessibleUserAsync(dietologistUserId, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure(userResult.Error);
        }

        Result<DietologistInvitationId> invitationIdResult = ParseInvitationId(command);
        if (invitationIdResult.IsFailure) {
            return RequiredIdParser.ToFailure(invitationIdResult);
        }

        DietologistInvitationId invitationId = invitationIdResult.Value;
        DietologistInvitation? invitation = await invitationRepository.GetByIdAsync(invitationId, asTracking: true, cancellationToken).ConfigureAwait(false);

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

        User user = userResult.Value;
        if (!string.Equals(invitation.DietologistEmail, user.Email, StringComparison.OrdinalIgnoreCase)) {
            return Result.Failure(Errors.Dietologist.InvitationNotFound);
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

    private static Result<DietologistInvitationId> ParseInvitationId(AcceptInvitationCommand command) =>
        RequiredIdParser.Parse(
            command.InvitationId,
            nameof(command.InvitationId),
            "Invitation id must not be empty.",
            value => new DietologistInvitationId(value));

    private static string ResolveDietologistDisplayName(User user) {
        string fullName = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? user.Email : fullName;
    }
}
