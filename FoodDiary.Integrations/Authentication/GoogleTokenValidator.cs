using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Results;
using FoodDiary.Integrations.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace FoodDiary.Integrations.Authentication;

public sealed class GoogleTokenValidator(IOptions<GoogleAuthOptions> options, ILogger<GoogleTokenValidator> logger)
    : IGoogleTokenValidator {
    private const string MetadataAddress = "https://accounts.google.com/.well-known/openid-configuration";
    private static readonly string[] ValidIssuers = ["https://accounts.google.com", "accounts.google.com"];
    private readonly GoogleAuthOptions _options = options.Value;
    private readonly IConfigurationManager<OpenIdConnectConfiguration> _configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            MetadataAddress,
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever { RequireHttps = true });

    internal GoogleTokenValidator(
        IOptions<GoogleAuthOptions> options,
        ILogger<GoogleTokenValidator> logger,
        IConfigurationManager<OpenIdConnectConfiguration> configurationManager)
        : this(options, logger) {
        _configurationManager = configurationManager;
    }

    public async Task<Result<GoogleIdentityPayload>> ValidateCredentialAsync(string credential, CancellationToken cancellationToken) {
        if (string.IsNullOrWhiteSpace(credential)) {
            return Result.Failure<GoogleIdentityPayload>(Errors.Validation.Required("credential"));
        }

        if (string.IsNullOrWhiteSpace(_options.ClientId)) {
            return Result.Failure<GoogleIdentityPayload>(Errors.Authentication.GoogleNotConfigured);
        }

        try {
            OpenIdConnectConfiguration configuration = await _configurationManager.GetConfigurationAsync(cancellationToken).ConfigureAwait(false);
            var tokenHandler = new JwtSecurityTokenHandler();
            ClaimsPrincipal principal = tokenHandler.ValidateToken(credential, new TokenValidationParameters {
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = configuration.SigningKeys,
                ValidateIssuer = true,
                ValidIssuers = ValidIssuers,
                ValidateAudience = true,
                ValidAudience = _options.ClientId,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1),
            }, out SecurityToken validatedToken);

            string? email = GetClaimValue(principal, ClaimTypes.Email) ??
                        GetClaimValue(principal, "email");
            if (string.IsNullOrWhiteSpace(email)) {
                return Result.Failure<GoogleIdentityPayload>(Errors.Authentication.GoogleInvalidToken);
            }

            string? emailVerified = GetClaimValue(principal, "email_verified");
            if (!bool.TryParse(emailVerified, out bool isEmailVerified) || !isEmailVerified) {
                return Result.Failure<GoogleIdentityPayload>(Errors.Authentication.GoogleEmailNotVerified);
            }

            string? issuer = validatedToken.Issuer;
            string? subject = GetClaimValue(principal, ClaimTypes.NameIdentifier) ?? GetClaimValue(principal, "sub");
            if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(subject)) {
                return Result.Failure<GoogleIdentityPayload>(Errors.Authentication.GoogleInvalidToken);
            }

            return Result.Success(new GoogleIdentityPayload(
                Issuer: NormalizeIssuer(issuer),
                Subject: subject,
                Email: email,
                FirstName: GetClaimValue(principal, ClaimTypes.GivenName) ?? GetClaimValue(principal, "given_name"),
                LastName: GetClaimValue(principal, ClaimTypes.Surname) ?? GetClaimValue(principal, "family_name"),
                Locale: GetClaimValue(principal, "locale")));
        } catch (Exception ex) when (ex is SecurityTokenException or InvalidOperationException or ArgumentException) {
            logger.LogWarning(ex, "Google credential validation failed.");
            return Result.Failure<GoogleIdentityPayload>(Errors.Authentication.GoogleInvalidToken);
        } catch (Exception ex) {
            logger.LogWarning(ex, "Google OpenID configuration retrieval failed.");
            return Result.Failure<GoogleIdentityPayload>(Errors.Authentication.GoogleInvalidToken);
        }
    }

    private static string? GetClaimValue(ClaimsPrincipal principal, string claimType) {
        return principal.FindFirst(claimType)?.Value;
    }

    private static string NormalizeIssuer(string issuer) =>
        string.Equals(issuer, "accounts.google.com", StringComparison.Ordinal)
            ? "https://accounts.google.com"
            : issuer;
}
