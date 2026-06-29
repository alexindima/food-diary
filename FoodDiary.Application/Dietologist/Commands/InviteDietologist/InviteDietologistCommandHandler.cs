using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
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
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.Entities.Notifications;

namespace FoodDiary.Application.Dietologist.Commands.InviteDietologist;

public class InviteDietologistCommandHandler(
    IDietologistInvitationRepository invitationRepository,
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IDietologistEmailSender emailSender,
    INotificationWriter notificationWriter,
    INotificationRepository notificationRepository,
    INotificationPusher notificationPusher,
    IPostCommitActionQueue postCommitActionQueue,
    TimeProvider dateTimeProvider,
    ILogger<InviteDietologistCommandHandler> logger)
    : ICommandHandler<InviteDietologistCommand, Result> {
    public async Task<Result> Handle(InviteDietologistCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);
        Error? accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure(accessError);
        }

        User user = (await userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false))!;
        string normalizedEmail = command.DietologistEmail.Trim().ToLowerInvariant();

        if (string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase)) {
            return Result.Failure(Errors.Dietologist.CannotInviteSelf);
        }

        DietologistInvitation? activeInvitation = await invitationRepository.GetActiveByClientAsync(userId, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (activeInvitation is not null) {
            return Result.Failure(Errors.Dietologist.AlreadyHasDietologist);
        }

        DietologistInvitation? pendingInvitation = await invitationRepository.GetByClientAndStatusAsync(
            userId, DietologistInvitationStatus.Pending, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (pendingInvitation is not null) {
            return Result.Failure(Errors.Dietologist.PendingInvitationExists);
        }

        string rawToken = SecurityTokenGenerator.GenerateUrlSafeToken();
        string tokenHash = passwordHasher.Hash(rawToken);
        DateTime expiresAt = dateTimeProvider.GetUtcNow().UtcDateTime.AddDays(7);
        DietologistPermissions permissions = command.Permissions.ToPermissions();

        var invitation = DietologistInvitation.Create(userId, normalizedEmail, tokenHash, expiresAt, permissions);
        User? registeredDietologist = await userRepository.GetByEmailAsync(normalizedEmail, cancellationToken).ConfigureAwait(false);
        await invitationRepository.AddAsync(invitation, cancellationToken).ConfigureAwait(false);
        EnqueueInvitationEmail(normalizedEmail, invitation, rawToken, user);

        if (registeredDietologist is not null) {
            await NotifyRegisteredDietologistAsync(registeredDietologist, user, invitation, cancellationToken).ConfigureAwait(false);
        }

        return Result.Success();
    }

    private void EnqueueInvitationEmail(
        string normalizedEmail,
        DietologistInvitation invitation,
        string rawToken,
        User user) {
        DietologistInvitationMessage message = new(
            normalizedEmail,
            invitation.Id.Value,
            rawToken,
            user.FirstName,
            user.LastName,
            user.Language);

        postCommitActionQueue.Enqueue(async ct => {
            try {
                await emailSender.SendDietologistInvitationAsync(message, ct).ConfigureAwait(false);
            } catch (Exception ex) {
                logger.LogWarning(ex, "Dietologist invitation email dispatch failed for {DietologistEmail}", normalizedEmail);
            }
        });
    }

    private async Task NotifyRegisteredDietologistAsync(
        User registeredDietologist,
        User client,
        DietologistInvitation invitation,
        CancellationToken cancellationToken) {
        Notification notification = NotificationFactory.CreateDietologistInvitationReceived(
            registeredDietologist.Id,
            ResolveClientName(client),
            invitation.Id.Value.ToString());

        await notificationWriter.AddAsync(notification, cancellationToken: cancellationToken).ConfigureAwait(false);
        NotificationPostCommitActions.EnqueueUnreadCountPush(
            postCommitActionQueue,
            notificationRepository,
            notificationPusher,
            registeredDietologist.Id);
    }

    private static string ResolveClientName(User user) {
        string clientName = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(clientName) ? user.Email : clientName;
    }
}
