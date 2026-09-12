using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Integrations.Options;
using FoodDiary.Results;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace FoodDiary.Integrations.Authentication;

public sealed class TelegramOidcTokenValidator(IOptions<TelegramOidcOptions> options, TimeProvider timeProvider)
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
        IConfigurationManager<OpenIdConnectConfiguration> configurationManager) : this(options, timeProvider) {
        _configurationManager = configurationManager;
    }

    public async Task<Result<TelegramOidcIdentity>> ValidateAsync(
        string idToken, string expectedNonce, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        if (!options.Value.Enabled || !TelegramOidcOptions.IsValid(options.Value)) {
            return Result.Failure<TelegramOidcIdentity>(TelegramIdentityErrors.NotConfigured);
        }
        if (string.IsNullOrWhiteSpace(idToken) || idToken.Length > MaxTokenLength ||
            string.IsNullOrWhiteSpace(expectedNonce) || expectedNonce.Length > 128) {
            return Invalid();
        }

        try {
            OpenIdConnectConfiguration configuration = await _configurationManager
                .GetConfigurationAsync(cancellationToken).ConfigureAwait(false);
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
            if (!string.Equals(nonce, expectedNonce, StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(subject) || subject.Length > 255 ||
                (authorizedParty is not null && !string.Equals(authorizedParty, options.Value.ClientId, StringComparison.Ordinal)) ||
                !long.TryParse(SingleClaim(principal, "id"), NumberStyles.None, CultureInfo.InvariantCulture, out long userId) || userId <= 0 ||
                !long.TryParse(SingleClaim(principal, "iat"), NumberStyles.None, CultureInfo.InvariantCulture, out long issuedAt) ||
                issuedAt > timeProvider.GetUtcNow().ToUnixTimeSeconds() ||
                principal.FindAll("aud").Count() != 1 || principal.FindAll("azp").Skip(1).Any()) {
                return Invalid();
            }

            return Result.Success(new TelegramOidcIdentity(Issuer, subject, userId,
                SingleClaim(principal, "given_name"), SingleClaim(principal, "family_name"),
                SingleClaim(principal, "preferred_username")));
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        } catch (SecurityTokenSignatureKeyNotFoundException) {
            _configurationManager.RequestRefresh();
            return Invalid();
        } catch (Exception ex) when (ex is SecurityTokenException or ArgumentException or InvalidOperationException or HttpRequestException or OperationCanceledException) {
            return Invalid();
        }
    }

    private static string? SingleClaim(ClaimsPrincipal principal, string type) {
        Claim[] claims = [.. principal.FindAll(type).Take(2)];
        return claims.Length == 1 ? claims[0].Value : null;
    }

    private static Result<TelegramOidcIdentity> Invalid() =>
        Result.Failure<TelegramOidcIdentity>(TelegramIdentityErrors.InvalidProof);
}
