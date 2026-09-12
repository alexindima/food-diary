using System.Net.Mail;
using System.Text.Json;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Identity.Authentication.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Identity.Authentication.Services;

public sealed class TelegramBackupEmailOidcService(ITelegramOidcProvider provider, ITelegramLoginTicketStore tickets,
    IUserAuthenticationIdentityService identities, TelegramBackupEmailService backupEmail, TimeProvider timeProvider) {
    private const string Purpose = "telegram-backup-email-oidc";

    public async Task<Result<TelegramOidcStartModel>> StartAsync(Guid userId, string email, string browserBinding, CancellationToken cancellationToken) {
        if (userId == Guid.Empty || browserBinding.Length != 43 || string.IsNullOrWhiteSpace(email) || email.Length > 254 ||
            !MailAddress.TryCreate(email, out MailAddress? address) || !string.Equals(address.Address, email.Trim(), StringComparison.OrdinalIgnoreCase)) {
            return Result.Failure<TelegramOidcStartModel>(TelegramIdentityErrors.InvalidProof);
        }
        if (!provider.IsEnabled) {
            return Result.Failure<TelegramOidcStartModel>(TelegramIdentityErrors.NotConfigured);
        }
        Result<UserAuthenticationPrincipalModel> principal = await identities.GetAuthenticationPrincipalAsync(new UserId(userId), timeProvider.GetUtcNow().UtcDateTime, cancellationToken).ConfigureAwait(false);
        if (principal.IsFailure || principal.Value.Email is not null || !principal.Value.User.HasTelegramIdentity) {
            return Result.Failure<TelegramOidcStartModel>(TelegramIdentityErrors.InvalidProof);
        }
        var attempt = new Attempt(userId, address.Address.Trim().ToLowerInvariant(), principal.Value.SecurityVersion,
            SecurityTokenGenerator.GenerateUrlSafeToken(), SecurityTokenGenerator.GenerateUrlSafeToken());
        string state = await tickets.CreateAsync(Purpose, Binding(browserBinding, userId), JsonSerializer.Serialize(attempt),
            timeProvider.GetUtcNow().UtcDateTime.AddMinutes(5), cancellationToken).ConfigureAwait(false);
        Result<string> url = provider.CreateAuthorizationUrl(state, attempt.Nonce, attempt.CodeVerifier);
        return url.IsSuccess ? Result.Success(new TelegramOidcStartModel(url.Value)) : Result.Failure<TelegramOidcStartModel>(url.Error);
    }

    public async Task<Result> CompleteAsync(Guid userId, string code, string state, string browserBinding, CancellationToken cancellationToken) {
        if (userId == Guid.Empty || browserBinding.Length != 43 || !provider.IsEnabled) {
            return Result.Failure(TelegramIdentityErrors.InvalidProof);
        }
        string? json = await tickets.ConsumeAsync(state, Purpose, Binding(browserBinding, userId), cancellationToken).ConfigureAwait(false);
        if (json is null) {
            return Result.Failure(TelegramIdentityErrors.InvalidProof);
        }
        Attempt? attempt;
        try {
            attempt = JsonSerializer.Deserialize<Attempt>(json);
        } catch (JsonException) {
            return Result.Failure(TelegramIdentityErrors.InvalidProof);
        }
        if (attempt is null || attempt.UserId != userId) {
            return Result.Failure(TelegramIdentityErrors.InvalidProof);
        }
        Result<TelegramOidcIdentity> identity = await provider.ExchangeAsync(code, attempt.CodeVerifier, attempt.Nonce, cancellationToken).ConfigureAwait(false);
        if (identity.IsFailure) {
            return Result.Failure(identity.Error);
        }
        return await backupEmail.RequestOidcAsync(userId, attempt.Email, identity.Value.TelegramUserId,
            attempt.SecurityVersion, cancellationToken).ConfigureAwait(false);
    }

    private static string Binding(string browserBinding, Guid userId) => $"{browserBinding}:{userId:D}";
    private sealed record Attempt(Guid UserId, string Email, long SecurityVersion, string Nonce, string CodeVerifier);
}
