using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Common.Utilities;
using Microsoft.Extensions.Logging;
using System;

namespace FoodDiary.Application.Authentication.Commands.RequestPasswordReset;

public sealed class RequestPasswordResetCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IEmailSender emailSender,
    ILogger<RequestPasswordResetCommandHandler> logger)
    : ICommandHandler<RequestPasswordResetCommand, Result<bool>>
{
    private static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(1);

    public async Task<Result<bool>> Handle(RequestPasswordResetCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailIncludingDeletedAsync(command.Email);
        if (user is null || !user.IsActive)
        {
            return Result.Success(true);
        }

        var nowUtc = DateTime.UtcNow;
        if (user.PasswordResetSentAtUtc.HasValue &&
            nowUtc - user.PasswordResetSentAtUtc.Value < Cooldown)
        {
            logger.LogInformation(
                "Password reset requested too soon for user {UserId}. Cooldown active.",
                user.Id.Value);
            return Result.Success(true);
        }

        var token = SecurityTokenGenerator.GenerateUrlSafeToken();
        var tokenHash = passwordHasher.Hash(token);
        var expiresAtUtc = nowUtc.Add(TokenLifetime);

        user.SetPasswordResetToken(tokenHash, expiresAtUtc);
        await userRepository.UpdateAsync(user);

        try
        {
            await emailSender.SendPasswordResetAsync(
                new PasswordResetMessage(user.Email, user.Id.Value.ToString(), token),
                cancellationToken);
            logger.LogInformation("Password reset email sent for user {UserId}.", user.Id.Value);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send password reset email for user {UserId}.", user.Id.Value);
            // Intentionally ignore to avoid leaking account existence via email failures.
        }

        return Result.Success(true);
    }
}
