using FoodDiary.Modules.Dietologist.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Dietologist.Domain.Enums;
using FoodDiary.Modules.Dietologist.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Dietologist.Application.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Common;
using FoodDiary.Modules.Notifications.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Dietologist.Domain.Entities;

namespace FoodDiary.Modules.Dietologist.Application.Commands.AcceptInvitationForCurrentUser;

public sealed class AcceptInvitationForCurrentUserCommandHandler(
    IDietologistInvitationWriteRepository invitationRepository,
    IDietologistUserContextService dietologistUserContextService,
    IUserRoleMembershipService userRoleMembershipService,
    INotificationWriter notificationWriter,
    INotificationClientRefreshService notificationClientRefreshService,
    IPostCommitActionQueue postCommitActionQueue)
    : ICommandHandler<AcceptInvitationForCurrentUserCommand, Result> {
    public async Task<Result> Handle(AcceptInvitationForCurrentUserCommand command, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            dietologistUserContextService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return Result.Failure(userIdResult.Error);
        }

        UserId dietologistUserId = userIdResult.Value;
        Result<UserDietologistProfileModel> userResult = await dietologistUserContextService.GetAccessibleProfileAsync(dietologistUserId, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure(userResult.Error);
        }

        UserDietologistProfileModel user = userResult.Value;
        Result<DietologistInvitationId> invitationIdResult = DietologistRequiredIdParser.Parse(
            command.InvitationId,
            nameof(command.InvitationId),
            "Invitation id must not be empty.",
            value => new DietologistInvitationId(value));
        if (invitationIdResult.IsFailure) {
            return DietologistRequiredIdParser.ToFailure(invitationIdResult);
        }

        DietologistInvitation? invitation = await invitationRepository.GetByIdAsync(
            invitationIdResult.Value,
            asTracking: true,
            cancellationToken).ConfigureAwait(false);
        if (invitation is null || invitation.Status != DietologistInvitationStatus.Pending) {
            return Result.Failure(DietologistErrors.InvitationNotFound);
        }

        if (!string.Equals(invitation.DietologistEmail, user.Email, StringComparison.OrdinalIgnoreCase)) {
            return Result.Failure(DietologistErrors.AccessDenied);
        }

        if (invitation.IsExpired()) {
            return Result.Failure(DietologistErrors.InvitationExpired);
        }

        invitation.Accept(dietologistUserId);

        if (!user.IsDietologist) {
            await userRoleMembershipService.EnsureRoleAsync(new UserId(user.Id), RoleNames.Dietologist, cancellationToken).ConfigureAwait(false);
        }

        await invitationRepository.UpdateAsync(invitation, cancellationToken).ConfigureAwait(false);
        await DietologistInvitationClientNotifier.NotifyAcceptedAsync(
            notificationWriter,
            notificationClientRefreshService,
            postCommitActionQueue,
            invitation.ClientUserId,
            ResolveDietologistDisplayName(user),
            invitation.Id.Value.ToString(),
            cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    private static string ResolveDietologistDisplayName(UserDietologistProfileModel user) {
        return DietologistProfileDisplayName.Resolve(user);
    }
}
