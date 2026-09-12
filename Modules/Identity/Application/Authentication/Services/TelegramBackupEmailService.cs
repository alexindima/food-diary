using System.Net.Mail;
using System.Text.Json;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Identity.Authentication.Services;

public sealed class TelegramBackupEmailService(ITelegramAuthValidator validator, ITelegramAssertionReplayGuard replay,
    IUserAuthenticationIdentityService identities, IUserTelegramAccountService accounts, ITelegramLoginTicketStore tickets,
    IEmailSender emailSender, TimeProvider timeProvider) {
    public const string TokenPrefix = "telegram-backup.";
    private const string Purpose = "telegram-backup-email";

    public async Task<Result> RequestAsync(Guid userId, string email, string initData, CancellationToken cancellationToken) {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(initData) || string.IsNullOrWhiteSpace(email) || email.Length > 254 ||
            !MailAddress.TryCreate(email, out MailAddress? address) || !string.Equals(address.Address, email.Trim(), StringComparison.OrdinalIgnoreCase)) {
            return Failure("Validation.Invalid", "A valid email address is required.", ErrorKind.Validation);
        }
        Result<TelegramInitData> proof = validator.ValidateInitData(initData);
        if (proof.IsFailure) {
            return Result.Failure(proof.Error);
        }
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        if (proof.Value.AuthDateUtc.Kind != DateTimeKind.Utc || proof.Value.AuthDateUtc > now || proof.Value.AuthDateUtc <= now.AddMinutes(-5)) {
            return InvalidProof();
        }
        Result<UserAuthenticationPrincipalModel> principal = await identities.AuthenticateTelegramAsync(proof.Value.UserId, now, cancellationToken).ConfigureAwait(false);
        if (principal.IsFailure) {
            return Result.Failure(principal.Error);
        }
        if (principal.Value.UserId.Value != userId || principal.Value.Email is not null) {
            return InvalidProof();
        }
        if (!await replay.TryConsumeAsync(initData, proof.Value.AuthDateUtc.AddDays(1), cancellationToken).ConfigureAwait(false)) {
            return InvalidProof();
        }
        return await SendAsync(principal.Value, address.Address, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result> RequestOidcAsync(Guid userId, string email, long telegramUserId, long securityVersion, CancellationToken cancellationToken) {
        Result<UserAuthenticationPrincipalModel> principal = await identities.AuthenticateTelegramAsync(
            telegramUserId, timeProvider.GetUtcNow().UtcDateTime, cancellationToken).ConfigureAwait(false);
        if (principal.IsFailure || principal.Value.UserId.Value != userId || principal.Value.Email is not null ||
            principal.Value.SecurityVersion != securityVersion) {
            return InvalidProof();
        }
        return await SendAsync(principal.Value, email, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Result> SendAsync(UserAuthenticationPrincipalModel principal, string email, CancellationToken cancellationToken) {
        Guid userId = principal.UserId.Value;
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        string payload = JsonSerializer.Serialize(new EmailProof(userId, email.Trim().ToLowerInvariant(), principal.SecurityVersion));
        string ticket = await tickets.CreateAsync(Purpose, userId.ToString("D"), payload, now.AddMinutes(15), cancellationToken).ConfigureAwait(false);
        await emailSender.SendEmailVerificationAsync(new EmailVerificationMessage(email, userId.ToString("D"),
            TokenPrefix + ticket, principal.User.Language), cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result> ConfirmAsync(Guid userId, string token, CancellationToken cancellationToken) {
        if (userId == Guid.Empty || !token.StartsWith(TokenPrefix, StringComparison.Ordinal)) {
            return InvalidProof();
        }
        string? payload = await tickets.ConsumeAsync(token[TokenPrefix.Length..], Purpose, userId.ToString("D"), cancellationToken).ConfigureAwait(false);
        if (payload is null) {
            return InvalidProof();
        }
        EmailProof? proof;
        try {
            proof = JsonSerializer.Deserialize<EmailProof>(payload);
        } catch (JsonException) {
            return InvalidProof();
        }
        if (proof is null || proof.UserId != userId || string.IsNullOrWhiteSpace(proof.Email)) {
            return InvalidProof();
        }
        return await accounts.AddVerifiedEmailAsync(new UserId(userId), proof.Email, proof.SecurityVersion, cancellationToken).ConfigureAwait(false);
    }

    private sealed record EmailProof(Guid UserId, string Email, long SecurityVersion);
    private static Result InvalidProof() => Failure("Authentication.TelegramProofRequired", "Request a new email confirmation from your Telegram account.", ErrorKind.Unauthorized);
    private static Result Failure(string code, string message, ErrorKind kind) => Result.Failure(new Error(code, message, kind));
}
