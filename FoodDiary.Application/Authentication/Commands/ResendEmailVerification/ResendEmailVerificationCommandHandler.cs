using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Application.Authentication.Commands.ResendEmailVerification;

public class ResendEmailVerificationCommandHandler : ICommandHandler<ResendEmailVerificationCommand, Result> {
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

    public async Task<Result> Handle(ResendEmailVerificationCommand command, CancellationToken cancellationToken) {
        if (command.UserId == Guid.Empty) {
            return Result.Failure(
                Errors.Validation.Invalid(nameof(command.UserId), "User id must not be empty."));
        }

        var user = await _userRepository.GetByIdAsync(new UserId(command.UserId), cancellationToken);
        var accessError = CurrentUserAccessPolicy.EnsureCanAccess(user);
        if (accessError is not null) {
            return Result.Failure(accessError);
        }

        var currentUser = user!;
        if (currentUser.IsEmailConfirmed) {
            return Result.Success();
        }

        if (currentUser.EmailConfirmationSentAtUtc.HasValue) {
            var elapsed = _dateTimeProvider.UtcNow - currentUser.EmailConfirmationSentAtUtc.Value;
            if (elapsed < ResendCooldown) {
                return Result.Failure(
                    Errors.Validation.Invalid(
                        "EmailVerification",
                        "Verification email was sent recently. Please wait before requesting a new one."));
            }
        }

        var emailToken = SecurityTokenGenerator.GenerateUrlSafeToken();
        var emailTokenHash = _passwordHasher.Hash(emailToken);
        currentUser.SetEmailConfirmationToken(new UserTokenIssue(
            TokenHash: emailTokenHash,
            ExpiresAtUtc: _dateTimeProvider.UtcNow.AddHours(24),
            IssuedAtUtc: _dateTimeProvider.UtcNow));
        await _userRepository.UpdateAsync(currentUser, cancellationToken);

        try {
            await _emailSender.SendEmailVerificationAsync(
                new EmailVerificationMessage(currentUser.Email, currentUser.Id.Value.ToString(), emailToken, currentUser.Language, command.ClientOrigin),
                cancellationToken);
        } catch (Exception ex) {
            _logger.LogError(ex, "Email verification dispatch failed.");
            return Result.Failure(
                Errors.Validation.Invalid(
                    "EmailVerification",
                    "Failed to send verification email."));
        }

        return Result.Success();
    }
}
