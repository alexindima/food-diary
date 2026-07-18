using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Results;
using FoodDiary.Integrations.Authentication;
using FoodDiary.Integrations.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace FoodDiary.Infrastructure.Tests.Integrations;

[ExcludeFromCodeCoverage]
public sealed class GoogleTokenValidatorTests {
    [Fact]
    public async Task ValidateCredentialAsync_WhenCredentialBlank_ReturnsRequiredFailure() {
        var validator = new GoogleTokenValidator(
            MsOptions.Create(new GoogleAuthOptions { ClientId = "client" }),
            NullLogger<GoogleTokenValidator>.Instance);

        Result<GoogleIdentityPayload> result = await validator.ValidateCredentialAsync("   ", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Required", result.Error.Code);
    }

    [Fact]
    public async Task ValidateCredentialAsync_WhenClientIdMissing_ReturnsNotConfigured() {
        var validator = new GoogleTokenValidator(
            MsOptions.Create(new GoogleAuthOptions { ClientId = "" }),
            NullLogger<GoogleTokenValidator>.Instance);

        Result<GoogleIdentityPayload> result = await validator.ValidateCredentialAsync("credential", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.GoogleNotConfigured", result.Error.Code);
    }

    [Fact]
    public void PrivateClaimHelper_ReturnsClaimValueAndNullForMissingClaim() {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("email", "user@example.com"),
        ]));

        Assert.Equal("user@example.com", InvokePrivateStatic<string?>("GetClaimValue", principal, "email"));
        Assert.Null(InvokePrivateStatic<string?>("GetClaimValue", principal, "missing"));
    }

    [Fact]
    public void ValidIssuers_ContainsGoogleIssuers() {
        System.Reflection.FieldInfo field = typeof(GoogleTokenValidator).GetField(
            "ValidIssuers",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;

        string[] issuers = Assert.IsType<string[]>(field.GetValue(null));

        Assert.Contains("https://accounts.google.com", issuers);
        Assert.Contains("accounts.google.com", issuers);
    }

    [Fact]
    public async Task ValidateCredentialAsync_WithValidGoogleJwt_ReturnsPayload() {
        const string clientId = "google-client";
        RsaSecurityKey signingKey = CreateSigningKey();
        string token = CreateGoogleJwt(signingKey, clientId, [
            new Claim("sub", "google-subject"),
            new Claim("email", "user@example.com"),
            new Claim("email_verified", "true"),
            new Claim("given_name", "Alex"),
            new Claim("family_name", "Smith"),
            new Claim("locale", "en"),
        ]);
        GoogleTokenValidator validator = CreateValidator(clientId, new StaticConfigurationManager(signingKey));

        Result<GoogleIdentityPayload> result = await validator.ValidateCredentialAsync(token, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.Equal("user@example.com", result.Value.Email);
        Assert.Equal("google-subject", result.Value.Subject);
        Assert.Equal("https://accounts.google.com", result.Value.Issuer);
        Assert.Equal("Alex", result.Value.FirstName);
        Assert.Equal("Smith", result.Value.LastName);
        Assert.Equal("en", result.Value.Locale);
    }

    [Fact]
    public async Task ValidateCredentialAsync_WhenEmailMissing_ReturnsInvalidToken() {
        const string clientId = "google-client";
        RsaSecurityKey signingKey = CreateSigningKey();
        string token = CreateGoogleJwt(signingKey, clientId, [
            new Claim("email_verified", "true"),
        ]);
        GoogleTokenValidator validator = CreateValidator(clientId, new StaticConfigurationManager(signingKey));

        Result<GoogleIdentityPayload> result = await validator.ValidateCredentialAsync(token, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.GoogleInvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task ValidateCredentialAsync_WhenEmailNotVerified_ReturnsEmailNotVerified() {
        const string clientId = "google-client";
        RsaSecurityKey signingKey = CreateSigningKey();
        string token = CreateGoogleJwt(signingKey, clientId, [
            new Claim("email", "user@example.com"),
            new Claim("email_verified", "false"),
        ]);
        GoogleTokenValidator validator = CreateValidator(clientId, new StaticConfigurationManager(signingKey));

        Result<GoogleIdentityPayload> result = await validator.ValidateCredentialAsync(token, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.GoogleEmailNotVerified", result.Error.Code);
    }

    [Fact]
    public async Task ValidateCredentialAsync_WhenTokenInvalid_ReturnsInvalidToken() {
        RsaSecurityKey signingKey = CreateSigningKey();
        GoogleTokenValidator validator = CreateValidator("google-client", new StaticConfigurationManager(signingKey));

        Result<GoogleIdentityPayload> result = await validator.ValidateCredentialAsync("not-a-jwt", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.GoogleInvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task ValidateCredentialAsync_WhenConfigurationRetrievalFails_ReturnsInvalidToken() {
        GoogleTokenValidator validator = CreateValidator(
            "google-client",
            new ThrowingConfigurationManager(new HttpRequestException("metadata unavailable")));

        Result<GoogleIdentityPayload> result = await validator.ValidateCredentialAsync("credential", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.GoogleInvalidToken", result.Error.Code);
    }

    private static T? InvokePrivateStatic<T>(string methodName, params object[] args) {
        System.Reflection.MethodInfo method = typeof(GoogleTokenValidator).GetMethod(
            methodName,
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
        return (T?)method.Invoke(null, args);
    }

    private static GoogleTokenValidator CreateValidator(
        string clientId,
        IConfigurationManager<OpenIdConnectConfiguration> configurationManager) =>
        new(
            MsOptions.Create(new GoogleAuthOptions { ClientId = clientId }),
            NullLogger<GoogleTokenValidator>.Instance,
            configurationManager);

    private static RsaSecurityKey CreateSigningKey() =>
        new(RSA.Create(2048)) { KeyId = Guid.NewGuid().ToString("N") };

    private static string CreateGoogleJwt(RsaSecurityKey signingKey, string clientId, IReadOnlyCollection<Claim> claims) {
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256);
        var token = new JwtSecurityToken(
            issuer: "https://accounts.google.com",
            audience: clientId,
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [ExcludeFromCodeCoverage]
    private sealed class StaticConfigurationManager(SecurityKey signingKey) : IConfigurationManager<OpenIdConnectConfiguration> {
        public Task<OpenIdConnectConfiguration> GetConfigurationAsync(CancellationToken cancel) {
            var configuration = new OpenIdConnectConfiguration();
            configuration.SigningKeys.Add(signingKey);
            return Task.FromResult(configuration);
        }

        public void RequestRefresh() {
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class ThrowingConfigurationManager(Exception exception) : IConfigurationManager<OpenIdConnectConfiguration> {
        public Task<OpenIdConnectConfiguration> GetConfigurationAsync(CancellationToken cancel) =>
            Task.FromException<OpenIdConnectConfiguration>(exception);

        public void RequestRefresh() {
        }
    }
}
