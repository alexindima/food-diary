using FoodDiary.Modules.Identity.Contracts.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using System.Net.Mail;
using System.Text.Json;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Services;

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
        Result<UserAuthenticationPrincipalModel> principal = await identities.AuthenticateTelegramAsync(proof.Value.UserId, now, cancellationToken);
        if (principal.IsFailure) {
            return Result.Failure(principal.Error);
        }
        if (principal.Value.UserId.Value != userId || principal.Value.Email is not null) {
            return InvalidProof();
        }
        if (!await replay.TryConsumeAsync(proof.Value.ReplayIdentity, proof.Value.ExpiresAtUtc, proof.Value.AuthDateUtc, cancellationToken)) {
            return InvalidProof();
        }
        return await SendAsync(principal.Value, address.Address, cancellationToken);
    }

    public async Task<Result> RequestOidcAsync(Guid userId, string email, long telegramUserId, long securityVersion, CancellationToken cancellationToken) {
        Result<UserAuthenticationPrincipalModel> principal = await identities.AuthenticateTelegramAsync(
            telegramUserId, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        if (principal.IsFailure || principal.Value.UserId.Value != userId || principal.Value.Email is not null ||
            principal.Value.SecurityVersion != securityVersion) {
            return InvalidProof();
        }
        return await SendAsync(principal.Value, email, cancellationToken);
    }

    private async Task<Result> SendAsync(UserAuthenticationPrincipalModel principal, string email, CancellationToken cancellationToken) {
        Guid userId = principal.UserId.Value;
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        string payload = JsonSerializer.Serialize(new EmailProof(userId, email.Trim().ToLowerInvariant(), principal.SecurityVersion));
        string ticket = await tickets.CreateAsync(Purpose, userId.ToString("D"), payload, now.AddMinutes(15), cancellationToken);
        await emailSender.SendEmailVerificationAsync(new EmailVerificationMessage(email, userId.ToString("D"),
            TokenPrefix + ticket, principal.User.Language), cancellationToken);
        return Result.Success();
    }

    public async Task<Result> ConfirmAsync(Guid userId, string token, CancellationToken cancellationToken) {
        EmailProof? proof = await ConsumeProofAsync(userId, token, cancellationToken);
        return proof is null
            ? InvalidProof()
            : await accounts.AddVerifiedEmailAsync(new UserId(userId), proof.Email, proof.SecurityVersion, cancellationToken);
    }

    internal async Task<EmailProof?> ConsumeProofAsync(Guid userId, string token, CancellationToken cancellationToken) {
        if (userId == Guid.Empty || !token.StartsWith(TokenPrefix, StringComparison.Ordinal)) {
            return null;
        }
        string? payload = await tickets.ConsumeAsync(token[TokenPrefix.Length..], Purpose, userId.ToString("D"), cancellationToken);
        if (payload is null) {
            return null;
        }
        EmailProof? proof;
        try {
            proof = JsonSerializer.Deserialize<EmailProof>(payload);
        } catch (JsonException) {
            return null;
        }
        return proof is not null && proof.UserId == userId && !string.IsNullOrWhiteSpace(proof.Email) ? proof : null;
    }

    internal sealed record EmailProof(Guid UserId, string Email, long SecurityVersion);
    private static Result InvalidProof() => Failure("Authentication.TelegramProofRequired", "Request a new email confirmation from your Telegram account.", ErrorKind.Unauthorized);
    private static Result Failure(string code, string message, ErrorKind kind) => Result.Failure(new Error(code, message, kind));
}
