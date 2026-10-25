using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Infrastructure.Providers.Options;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FoodDiary.Results;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace FoodDiary.Modules.Identity.Infrastructure.Providers.Authentication;

public sealed class TelegramOidcTokenValidator(IOptions<TelegramOidcOptions> options, TimeProvider timeProvider,
    ILogger<TelegramOidcTokenValidator> logger)
    : ITelegramOidcTokenValidator {
    private const string Issuer = "https://oauth.telegram.org";
    private const int MaxTokenLength = 16384;
    private readonly IConfigurationManager<OpenIdConnectConfiguration> _configurationManager =
        new ConfigurationManager<OpenIdConnectConfiguration>(
            Issuer + "/.well-known/openid-configuration",
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever { RequireHttps = true });

    internal TelegramOidcTokenValidator(
        IOptions<TelegramOidcOptions> options,
        TimeProvider timeProvider,
        IConfigurationManager<OpenIdConnectConfiguration> configurationManager, ILogger<TelegramOidcTokenValidator> logger) : this(options, timeProvider, logger) {
        _configurationManager = configurationManager;
    }

    public async Task<Result<TelegramOidcIdentity>> ValidateAsync(
        string idToken, TelegramOidcNonce expectedNonce, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        if (!options.Value.Enabled || !TelegramOidcOptions.IsValid(options.Value)) {
            LogRejection("token_validation", "not_configured");
            return Result.Failure<TelegramOidcIdentity>(TelegramIdentityErrors.NotConfigured);
        }
        if (string.IsNullOrWhiteSpace(idToken) || idToken.Length > MaxTokenLength ||
            string.IsNullOrWhiteSpace(expectedNonce.Value) || expectedNonce.Value.Length > 128) {
            return Invalid("token_validation", "invalid_input");
        }

        string stage = "metadata";
        try {
            OpenIdConnectConfiguration configuration = await _configurationManager
                .GetConfigurationAsync(cancellationToken).ConfigureAwait(false);
            stage = "token_validation";
            var handler = new JwtSecurityTokenHandler { MapInboundClaims = false, MaximumTokenSizeInBytes = MaxTokenLength };
            ClaimsPrincipal principal = handler.ValidateToken(idToken, new TokenValidationParameters {
                RequireSignedTokens = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = configuration.SigningKeys,
                ValidAlgorithms = [SecurityAlgorithms.RsaSha256, SecurityAlgorithms.EcdsaSha256],
                ValidateIssuer = true,
                ValidIssuer = Issuer,
                ValidateAudience = true,
                ValidAudience = options.Value.ClientId,
                RequireExpirationTime = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                LifetimeValidator = (notBefore, expires, _, _) => {
                    DateTime now = timeProvider.GetUtcNow().UtcDateTime;
                    return expires > now && (!notBefore.HasValue || notBefore.Value <= now);
                },
            }, out _);

            string? nonce = SingleClaim(principal, "nonce");
            string? subject = SingleClaim(principal, "sub");
            string? authorizedParty = SingleClaim(principal, "azp");
            if (nonce is null) {
                return Invalid(stage, "nonce_missing_or_duplicate");
            }
            if (!string.Equals(nonce, expectedNonce.Value, StringComparison.Ordinal)) {
                return Invalid(stage, "nonce_mismatch");
            }
            if (string.IsNullOrWhiteSpace(subject) || subject.Length > 255) {
                return Invalid(stage, "subject_invalid");
            }
            if (authorizedParty is not null && !string.Equals(authorizedParty, options.Value.ClientId, StringComparison.Ordinal)) {
                return Invalid(stage, "authorized_party_mismatch");
            }
            if (!long.TryParse(SingleClaim(principal, "id"), NumberStyles.None, CultureInfo.InvariantCulture, out long userId) || userId <= 0) {
                return Invalid(stage, "telegram_id_invalid");
            }
            if (!long.TryParse(SingleClaim(principal, "iat"), NumberStyles.None, CultureInfo.InvariantCulture, out long issuedAt)) {
                return Invalid(stage, "issued_at_invalid");
            }
            if (issuedAt > timeProvider.GetUtcNow().ToUnixTimeSeconds()) {
                return Invalid(stage, "issued_at_in_future");
            }
            if (principal.FindAll("aud").Count() != 1) {
                return Invalid(stage, "audience_count_invalid");
            }
            if (principal.FindAll("azp").Skip(1).Any()) {
                return Invalid(stage, "authorized_party_duplicate");
            }

            return Result.Success(new TelegramOidcIdentity(Issuer, subject, userId,
                SingleClaim(principal, "given_name"), SingleClaim(principal, "family_name"),
                SingleClaim(principal, "preferred_username")));
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        } catch (SecurityTokenSignatureKeyNotFoundException ex) {
            _configurationManager.RequestRefresh();
            return Invalid(stage, "signing_key_not_found", ex);
        } catch (Exception ex) when (ex is SecurityTokenException or ArgumentException or InvalidOperationException or HttpRequestException or OperationCanceledException) {
            string reason = string.Equals(stage, "metadata", StringComparison.Ordinal) ? "metadata_unavailable" : ex switch {
                SecurityTokenExpiredException => "token_expired",
                SecurityTokenNotYetValidException => "token_not_yet_valid",
                SecurityTokenInvalidIssuerException => "issuer_invalid",
                SecurityTokenInvalidAudienceException => "audience_invalid",
                SecurityTokenInvalidSignatureException => "signature_invalid",
                SecurityTokenInvalidAlgorithmException => "algorithm_invalid",
                SecurityTokenNoExpirationException => "expiration_missing",
                SecurityTokenInvalidLifetimeException lifetime when lifetime.Expires <= timeProvider.GetUtcNow().UtcDateTime => "token_expired",
                SecurityTokenInvalidLifetimeException lifetime when lifetime.NotBefore > timeProvider.GetUtcNow().UtcDateTime => "token_not_yet_valid",
                SecurityTokenInvalidLifetimeException => "lifetime_invalid",
                _ => "token_invalid",
            };
            return Invalid(stage, reason, ex);
        }
    }

    private static string? SingleClaim(ClaimsPrincipal principal, string type) {
        Claim[] claims = [.. principal.FindAll(type).Take(2)];
        return claims.Length == 1 ? claims[0].Value : null;
    }

    private Result<TelegramOidcIdentity> Invalid(string stage, string reason, Exception? exception = null) {
        LogRejection(stage, reason, exception);
        return Result.Failure<TelegramOidcIdentity>(TelegramIdentityErrors.InvalidProof);
    }

    private void LogRejection(string stage, string reason, Exception? exception = null) =>
        logger.LogInformation("Telegram OIDC rejected. Stage={Stage}; Reason={Reason}; ExceptionType={ExceptionType}",
            stage, reason, exception?.GetBaseException().GetType().Name);
}
