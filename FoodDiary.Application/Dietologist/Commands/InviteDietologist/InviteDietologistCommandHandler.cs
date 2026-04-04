using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Mappings;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Commands.InviteDietologist;

public class InviteDietologistCommandHandler(
    IDietologistInvitationRepository invitationRepository,
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IDietologistEmailSender emailSender,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<InviteDietologistCommand, Result> {
    public async Task<Result> Handle(InviteDietologistCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken);
        if (accessError is not null) {
            return Result.Failure(accessError);
        }

        var user = (await userRepository.GetByIdAsync(userId, cancellationToken))!;
        var normalizedEmail = command.DietologistEmail.Trim().ToLowerInvariant();

        if (string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase)) {
            return Result.Failure(Errors.Dietologist.CannotInviteSelf);
        }

        var activeInvitation = await invitationRepository.GetActiveByClientAsync(userId, cancellationToken: cancellationToken);
        if (activeInvitation is not null) {
            return Result.Failure(Errors.Dietologist.AlreadyHasDietologist);
        }

        var pendingInvitation = await invitationRepository.GetByClientAndStatusAsync(
            userId, DietologistInvitationStatus.Pending, cancellationToken: cancellationToken);
        if (pendingInvitation is not null) {
            return Result.Failure(Errors.Dietologist.PendingInvitationExists);
        }

        var rawToken = SecurityTokenGenerator.GenerateUrlSafeToken();
        var tokenHash = passwordHasher.Hash(rawToken);
        var expiresAt = dateTimeProvider.UtcNow.AddDays(7);
        var permissions = command.Permissions.ToPermissions();

        var invitation = DietologistInvitation.Create(userId, normalizedEmail, tokenHash, expiresAt, permissions);
        await invitationRepository.AddAsync(invitation, cancellationToken);

        await emailSender.SendDietologistInvitationAsync(
            new DietologistInvitationMessage(
                normalizedEmail,
                invitation.Id.Value,
                rawToken,
                user.FirstName,
                user.LastName,
                user.Language),
            cancellationToken);

        return Result.Success();
    }
}
