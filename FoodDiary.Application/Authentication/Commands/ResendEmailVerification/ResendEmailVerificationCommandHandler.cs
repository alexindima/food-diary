using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Common.Utilities;
using Microsoft.Extensions.Logging;
using static FoodDiary.Application.Common.Abstractions.Result.Errors;
using System;

namespace FoodDiary.Application.Authentication.Commands.ResendEmailVerification;

public class ResendEmailVerificationCommandHandler : ICommandHandler<ResendEmailVerificationCommand, Result<bool>>
{
    private static readonly TimeSpan ResendCooldown = TimeSpan.FromMinutes(1);
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<ResendEmailVerificationCommandHandler> _logger;

    public ResendEmailVerificationCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IEmailSender emailSender,
        ILogger<ResendEmailVerificationCommandHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(ResendEmailVerificationCommand command, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(command.UserId);
        if (user is null)
        {
            return Result.Failure<bool>(User.NotFound());
        }

        if (user.IsEmailConfirmed)
        {
            return Result.Success(true);
        }

        if (user.EmailConfirmationSentAtUtc.HasValue)
        {
            var elapsed = DateTime.UtcNow - user.EmailConfirmationSentAtUtc.Value;
            if (elapsed < ResendCooldown)
            {
                return Result.Failure<bool>(
                    Validation.Invalid(
                        "EmailVerification",
                        "Verification email was sent recently. Please wait before requesting a new one."));
            }
        }

        var emailToken = SecurityTokenGenerator.GenerateUrlSafeToken();
        var emailTokenHash = _passwordHasher.Hash(emailToken);
        user.SetEmailConfirmationToken(emailTokenHash, DateTime.UtcNow.AddHours(24));
        await _userRepository.UpdateAsync(user);

        try
        {
            await _emailSender.SendEmailVerificationAsync(
                new EmailVerificationMessage(user.Email, user.Id.Value.ToString(), emailToken, user.Language),
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email verification for user {UserId}.", user.Id.Value);
            return Result.Failure<bool>(
                Validation.Invalid(
                    "EmailVerification",
                    "Failed to send verification email."));
        }

        return Result.Success(true);
    }
}
