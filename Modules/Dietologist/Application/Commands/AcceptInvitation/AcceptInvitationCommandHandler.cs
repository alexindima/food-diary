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
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Dietologist.Domain.Entities;
using FoodDiary.Modules.Users.Contracts.Models;

namespace FoodDiary.Modules.Dietologist.Application.Commands.AcceptInvitation;

public sealed class AcceptInvitationCommandHandler(
    IDietologistInvitationWriteRepository invitationRepository,
    IDietologistUserContextService dietologistUserContextService,
    IUserRoleMembershipService userRoleMembershipService,
    IPasswordHasher passwordHasher,
    INotificationWriter notificationWriter,
    INotificationClientRefreshService notificationClientRefreshService,
    IPostCommitActionQueue postCommitActionQueue)
    : ICommandHandler<AcceptInvitationCommand, Result> {
    public async Task<Result> Handle(AcceptInvitationCommand command, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await ResolveUserIdAsync(command, cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return Result.Failure(userIdResult.Error);
        }

        UserId dietologistUserId = userIdResult.Value;
        Result<UserDietologistProfileModel> userResult = await dietologistUserContextService.GetAccessibleProfileAsync(dietologistUserId, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure(userResult.Error);
        }

        Result<DietologistInvitationId> invitationIdResult = ParseInvitationId(command);
        if (invitationIdResult.IsFailure) {
            return DietologistRequiredIdParser.ToFailure(invitationIdResult);
        }

        DietologistInvitationId invitationId = invitationIdResult.Value;
        DietologistInvitation? invitation = await invitationRepository.GetByIdAsync(invitationId, asTracking: true, cancellationToken).ConfigureAwait(false);

        if (invitation is null) {
            return Result.Failure(DietologistErrors.InvitationNotFound);
        }

        if (invitation.Status != DietologistInvitationStatus.Pending) {
            return Result.Failure(DietologistErrors.InvitationNotFound);
        }

        if (invitation.IsExpired()) {
            return Result.Failure(DietologistErrors.InvitationExpired);
        }

        if (!passwordHasher.Verify(command.Token, invitation.TokenHash)) {
            return Result.Failure(DietologistErrors.InvitationInvalidToken);
        }

        UserDietologistProfileModel user = userResult.Value;
        if (!string.Equals(invitation.DietologistEmail, user.Email, StringComparison.OrdinalIgnoreCase)) {
            return Result.Failure(DietologistErrors.InvitationNotFound);
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

    private Task<Result<UserId>> ResolveUserIdAsync(
        AcceptInvitationCommand command,
        CancellationToken cancellationToken) =>
        CurrentUserAccessResolver.ResolveAsync(command.UserId, dietologistUserContextService, cancellationToken);

    private static Result<DietologistInvitationId> ParseInvitationId(AcceptInvitationCommand command) =>
        DietologistRequiredIdParser.Parse(
            command.InvitationId,
            nameof(command.InvitationId),
            "Invitation id must not be empty.",
            value => new DietologistInvitationId(value));

    private static string ResolveDietologistDisplayName(UserDietologistProfileModel user) {
        return DietologistProfileDisplayName.Resolve(user);
    }
}
