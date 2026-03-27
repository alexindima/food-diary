using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Application.Authentication.Commands.ResendEmailVerification;

public class ResendEmailVerificationCommandHandler : ICommandHandler<ResendEmailVerificationCommand, Result<bool>> {
    private static readonly TimeSpan ResendCooldown = TimeSpan.FromMinutes(1);
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailSender _emailSender;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<ResendEmailVerificationCommandHandler> _logger;

    public ResendEmailVerificationCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IEmailSender emailSender,
        IDateTimeProvider dateTimeProvider,
        ILogger<ResendEmailVerificationCommandHandler> logger) {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _emailSender = emailSender;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(ResendEmailVerificationCommand command, CancellationToken cancellationToken) {
        var user = await _userRepository.GetByIdAsync(new UserId(command.UserId), cancellationToken);
        if (user is null) {
            return Result.Failure<bool>(Errors.User.NotFound());
        }

        if (user.IsEmailConfirmed) {
            return Result.Success(true);
        }

        if (user.EmailConfirmationSentAtUtc.HasValue) {
            var elapsed = _dateTimeProvider.UtcNow - user.EmailConfirmationSentAtUtc.Value;
            if (elapsed < ResendCooldown) {
                return Result.Failure<bool>(
                    Errors.Validation.Invalid(
                        "EmailVerification",
                        "Verification email was sent recently. Please wait before requesting a new one."));
            }
        }

        var emailToken = SecurityTokenGenerator.GenerateUrlSafeToken();
        var emailTokenHash = _passwordHasher.Hash(emailToken);
        user.SetEmailConfirmationToken(emailTokenHash, _dateTimeProvider.UtcNow.AddHours(24));
        await _userRepository.UpdateAsync(user, cancellationToken);

        try {
            await _emailSender.SendEmailVerificationAsync(
                new EmailVerificationMessage(user.Email, user.Id.Value.ToString(), emailToken, user.Language),
                cancellationToken);
        } catch (Exception ex) {
            _logger.LogError(ex, "Email verification dispatch failed.");
            return Result.Failure<bool>(
                Errors.Validation.Invalid(
                    "EmailVerification",
                    "Failed to send verification email."));
        }

        return Result.Success(true);
    }
}
