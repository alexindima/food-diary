using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Application.Authentication.Commands.RequestPasswordReset;

public sealed class RequestPasswordResetCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IEmailSender emailSender,
    IDateTimeProvider dateTimeProvider,
    ILogger<RequestPasswordResetCommandHandler> logger)
    : ICommandHandler<RequestPasswordResetCommand, Result<bool>> {
    private static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(1);

    public async Task<Result<bool>> Handle(RequestPasswordResetCommand command, CancellationToken cancellationToken) {
        var user = await userRepository.GetByEmailIncludingDeletedAsync(command.Email, cancellationToken);
        if (user is null || !user.IsActive) {
            return Result.Success(true);
        }

        var nowUtc = dateTimeProvider.UtcNow;
        if (user.PasswordResetSentAtUtc.HasValue &&
            nowUtc - user.PasswordResetSentAtUtc.Value < Cooldown) {
            logger.LogInformation("Password reset request throttled by cooldown.");
            return Result.Success(true);
        }

        var token = SecurityTokenGenerator.GenerateUrlSafeToken();
        var tokenHash = passwordHasher.Hash(token);
        var expiresAtUtc = nowUtc.Add(TokenLifetime);

        user.SetPasswordResetToken(tokenHash, expiresAtUtc, nowUtc);
        await userRepository.UpdateAsync(user, cancellationToken);

        try {
            await emailSender.SendPasswordResetAsync(
                new PasswordResetMessage(user.Email, user.Id.Value.ToString(), token, user.Language),
                cancellationToken);
            logger.LogInformation("Password reset email dispatch completed.");
        } catch (Exception ex) {
            logger.LogWarning(ex, "Password reset email dispatch failed.");
            // Intentionally ignore to avoid leaking account existence via email failures.
        }

        return Result.Success(true);
    }
}
