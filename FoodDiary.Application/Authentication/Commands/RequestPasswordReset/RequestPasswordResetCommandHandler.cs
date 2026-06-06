using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Authentication.Commands.RequestPasswordReset;

public sealed class RequestPasswordResetCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IEmailSender emailSender,
    IDateTimeProvider dateTimeProvider,
    ILogger<RequestPasswordResetCommandHandler> logger)
    : ICommandHandler<RequestPasswordResetCommand, Result> {
    private static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(1);

    public async Task<Result> Handle(RequestPasswordResetCommand command, CancellationToken cancellationToken) {
        User? user = await userRepository.GetByEmailIncludingDeletedAsync(command.Email, cancellationToken).ConfigureAwait(false);
        if (!AuthenticationUserAccessPolicy.CanRequestPasswordReset(user)) {
            return Result.Success();
        }

        User currentUser = user!;
        DateTime nowUtc = dateTimeProvider.UtcNow;
        if (currentUser.PasswordResetSentAtUtc.HasValue &&
            nowUtc - currentUser.PasswordResetSentAtUtc.Value < Cooldown) {
            logger.LogInformation("Password reset request throttled by cooldown.");
            return Result.Success();
        }

        string token = SecurityTokenGenerator.GenerateUrlSafeToken();
        string tokenHash = passwordHasher.Hash(token);
        DateTime expiresAtUtc = nowUtc.Add(TokenLifetime);

        currentUser.SetPasswordResetToken(new UserTokenIssue(
            TokenHash: tokenHash,
            ExpiresAtUtc: expiresAtUtc,
            IssuedAtUtc: nowUtc));
        await userRepository.UpdateAsync(currentUser, cancellationToken).ConfigureAwait(false);

        try {
            await emailSender.SendPasswordResetAsync(
                new PasswordResetMessage(currentUser.Email, currentUser.Id.Value.ToString(), token, currentUser.Language, command.ClientOrigin),
                cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Password reset email dispatch completed.");
        } catch (Exception ex) {
            logger.LogWarning(ex, "Password reset email dispatch failed.");
            // Intentionally ignore to avoid leaking account existence via email failures.
        }

        return Result.Success();
    }
}
