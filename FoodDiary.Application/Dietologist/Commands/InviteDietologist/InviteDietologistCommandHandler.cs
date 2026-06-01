using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Dietologist.Mappings;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.Extensions.Logging;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Dietologist.Commands.InviteDietologist;

public class InviteDietologistCommandHandler(
    IDietologistInvitationRepository invitationRepository,
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IDietologistEmailSender emailSender,
    INotificationRepository notificationRepository,
    INotificationPusher notificationPusher,
    IDateTimeProvider dateTimeProvider,
    ILogger<InviteDietologistCommandHandler> logger)
    : ICommandHandler<InviteDietologistCommand, Result> {
    public async Task<Result> Handle(InviteDietologistCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure(accessError);
        }

        var user = (await userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false))!;
        var normalizedEmail = command.DietologistEmail.Trim().ToLowerInvariant();

        if (string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase)) {
            return Result.Failure(Errors.Dietologist.CannotInviteSelf);
        }

        var activeInvitation = await invitationRepository.GetActiveByClientAsync(userId, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (activeInvitation is not null) {
            return Result.Failure(Errors.Dietologist.AlreadyHasDietologist);
        }

        var pendingInvitation = await invitationRepository.GetByClientAndStatusAsync(
            userId, DietologistInvitationStatus.Pending, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (pendingInvitation is not null) {
            return Result.Failure(Errors.Dietologist.PendingInvitationExists);
        }

        var rawToken = SecurityTokenGenerator.GenerateUrlSafeToken();
        var tokenHash = passwordHasher.Hash(rawToken);
        var expiresAt = dateTimeProvider.UtcNow.AddDays(7);
        var permissions = command.Permissions.ToPermissions();

        var invitation = DietologistInvitation.Create(userId, normalizedEmail, tokenHash, expiresAt, permissions);
        var registeredDietologist = await userRepository.GetByEmailAsync(normalizedEmail, cancellationToken).ConfigureAwait(false);
        var emailSent = await TrySendInvitationEmailAsync(normalizedEmail, invitation, rawToken, user, cancellationToken).ConfigureAwait(false);

        if (!emailSent && registeredDietologist is null) {
            return Result.Failure(Errors.Validation.Invalid(
                nameof(command.DietologistEmail),
                "Failed to deliver dietologist invitation email."));
        }

        await invitationRepository.AddAsync(invitation, cancellationToken).ConfigureAwait(false);

        if (registeredDietologist is not null) {
            await NotifyRegisteredDietologistAsync(registeredDietologist, user, invitation, cancellationToken).ConfigureAwait(false);
        }

        return Result.Success();
    }

    private async Task<bool> TrySendInvitationEmailAsync(
        string normalizedEmail,
        DietologistInvitation invitation,
        string rawToken,
        User user,
        CancellationToken cancellationToken) {
        try {
            await emailSender.SendDietologistInvitationAsync(
                new DietologistInvitationMessage(
                    normalizedEmail,
                    invitation.Id.Value,
                    rawToken,
                    user.FirstName,
                    user.LastName,
                    user.Language),
                cancellationToken).ConfigureAwait(false);
            return true;
        } catch (Exception ex) {
            logger.LogWarning(ex, "Dietologist invitation email dispatch failed for {DietologistEmail}", normalizedEmail);
            return false;
        }
    }

    private async Task NotifyRegisteredDietologistAsync(
        User registeredDietologist,
        User client,
        DietologistInvitation invitation,
        CancellationToken cancellationToken) {
        var notification = NotificationFactory.CreateDietologistInvitationReceived(
            registeredDietologist.Id,
            ResolveClientName(client),
            invitation.Id.Value.ToString());

        await notificationRepository.AddAsync(notification, cancellationToken).ConfigureAwait(false);
        var unreadCount = await notificationRepository.GetUnreadCountAsync(registeredDietologist.Id, cancellationToken).ConfigureAwait(false);
        await notificationPusher.PushUnreadCountAsync(registeredDietologist.Id.Value, unreadCount, cancellationToken).ConfigureAwait(false);
        await notificationPusher.PushNotificationsChangedAsync(registeredDietologist.Id.Value, cancellationToken).ConfigureAwait(false);
    }

    private static string ResolveClientName(User user) {
        var clientName = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(clientName) ? user.Email : clientName;
    }
}
