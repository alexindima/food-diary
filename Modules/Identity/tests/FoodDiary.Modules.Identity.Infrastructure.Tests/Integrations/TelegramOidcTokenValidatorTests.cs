using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Infrastructure.Providers.Authentication;
using FoodDiary.Modules.Identity.Infrastructure.Providers.Options;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using FoodDiary.Results;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace FoodDiary.Modules.Identity.Infrastructure.Tests.Integrations;

[ExcludeFromCodeCoverage]
public sealed class TelegramOidcTokenValidatorTests {
    [Theory]
    [InlineData(0, 5)]
    [InlineData(16385, 5)]
    [InlineData(5, 0)]
    [InlineData(5, 129)]
    public async Task InvalidInput_DoesNotFetchMetadata(int tokenLength, int nonceLength) {
        var configuration = new StaticConfiguration(signingKey: null);
        var logger = new TelegramOidcTestLogger<TelegramOidcTokenValidator>();
        Result<TelegramOidcIdentity> result = await CreateValidator(configuration, logger)
            .ValidateAsync(new string('x', tokenLength), new TelegramOidcNonce(new string('n', nonceLength)), CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Equal(0, configuration.FetchCount);
        logger.AssertRejection("token_validation", "invalid_input");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MetadataFailure_RejectsProofButPropagatesCallerCancellation(bool callerCancelled) {
        using var cancellation = new CancellationTokenSource();
        var configuration = new StaticConfiguration(signingKey: null) {
            OnFetch = async () => {
                if (callerCancelled) {
                    await cancellation.CancelAsync();
                    throw new OperationCanceledException(cancellation.Token);
                }
                throw new HttpRequestException("Metadata unavailable");
            },
        };
        var logger = new TelegramOidcTestLogger<TelegramOidcTokenValidator>();
        TelegramOidcTokenValidator validator = CreateValidator(configuration, logger);
        if (callerCancelled) {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => validator.ValidateAsync("token", new TelegramOidcNonce("nonce"), cancellation.Token));
            Assert.Empty(logger.Entries);
        } else {
            Assert.True((await validator.ValidateAsync("token", new TelegramOidcNonce("nonce"), cancellation.Token)).IsFailure);
            logger.AssertRejection("metadata", "metadata_unavailable", "Metadata unavailable");
        }
        Assert.Equal(1, configuration.FetchCount);
    }

    private static readonly DateTime Now = new(2026, 9, 12, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ValidToken_PreservesDistinctSubjectAndTelegramId() {
        using var rsa = RSA.Create(2048);
        var key = new RsaSecurityKey(rsa) { KeyId = "key" };
        var logger = new TelegramOidcTestLogger<TelegramOidcTokenValidator>();
        TelegramOidcTokenValidator validator = CreateValidator(new StaticConfiguration(key), logger);

        Result<TelegramOidcIdentity> result = await validator.ValidateAsync(CreateToken(key), new TelegramOidcNonce("nonce"), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.Equal(987654321, result.Value.TelegramUserId);
        Assert.Equal("1234123412341234123", result.Value.Subject);
        Assert.Equal("Alex", result.Value.FirstName);
        Assert.Empty(logger.Entries);
    }

    [Theory]
    [InlineData("nonce", "wrong", "nonce_mismatch")]
    [InlineData("sub", "", "subject_invalid")]
    [InlineData("id", "-1", "telegram_id_invalid")]
    [InlineData("id", "not-a-number", "telegram_id_invalid")]
    [InlineData("id", "9223372036854775808", "telegram_id_invalid")]
    [InlineData("iat", "9999999999", "issued_at_in_future")]
    [InlineData("azp", "other-client", "authorized_party_mismatch")]
    public async Task InvalidClaim_IsRejected(string claimType, string value, string reason) {
        using var rsa = RSA.Create(2048);
        var key = new RsaSecurityKey(rsa) { KeyId = "key" };
        List<Claim> claims = Claims();
        claims.RemoveAll(claim => string.Equals(claim.Type, claimType, StringComparison.Ordinal));
        claims.Add(new Claim(claimType, value));

        var logger = new TelegramOidcTestLogger<TelegramOidcTokenValidator>();
        string token = CreateToken(key, claims);
        Result<TelegramOidcIdentity> result = await CreateValidator(new StaticConfiguration(key), logger)
            .ValidateAsync(token, new TelegramOidcNonce("nonce"), CancellationToken.None);

        Assert.True(result.IsFailure);
        logger.AssertRejection("token_validation", reason, token, "1234123412341234123", "987654321", "test-secret");
    }

    [Theory]
    [InlineData("nonce", "nonce_missing_or_duplicate")]
    [InlineData("id", "telegram_id_invalid")]
    [InlineData("sub", "subject_invalid")]
    [InlineData("iat", "issued_at_invalid")]
    public async Task MissingRequiredClaim_IsRejected(string claimType, string reason) {
        using var rsa = RSA.Create(2048);
        var key = new RsaSecurityKey(rsa) { KeyId = "key" };
        List<Claim> claims = Claims();
        claims.RemoveAll(claim => string.Equals(claim.Type, claimType, StringComparison.Ordinal));
        var logger = new TelegramOidcTestLogger<TelegramOidcTokenValidator>();
        Result<TelegramOidcIdentity> result = await CreateValidator(new StaticConfiguration(key), logger)
            .ValidateAsync(CreateToken(key, claims), new TelegramOidcNonce("nonce"), CancellationToken.None);
        Assert.True(result.IsFailure);
        logger.AssertRejection("token_validation", reason);
    }

    [Theory]
    [InlineData("issuer", "issuer_invalid")]
    [InlineData("audience", "audience_invalid")]
    [InlineData("expiry", "token_expired")]
    [InlineData("not-before", "token_not_yet_valid")]
    [InlineData("duplicate-nonce", "nonce_missing_or_duplicate")]
    public async Task InvalidTokenBoundary_IsRejected(string boundary, string reason) {
        using var rsa = RSA.Create(2048);
        var key = new RsaSecurityKey(rsa) { KeyId = "key" };
        List<Claim> claims = Claims();
        if (string.Equals(boundary, "duplicate-nonce", StringComparison.Ordinal)) {
            claims.Add(new Claim("nonce", "another"));
        }
        var token = new JwtSecurityToken(
            issuer: string.Equals(boundary, "issuer", StringComparison.Ordinal) ? "https://attacker.invalid" : "https://oauth.telegram.org",
            audience: string.Equals(boundary, "audience", StringComparison.Ordinal) ? "other" : "123456",
            claims: claims,
            notBefore: string.Equals(boundary, "not-before", StringComparison.Ordinal) ? Now.AddMinutes(1) : Now.AddMinutes(-1),
            expires: string.Equals(boundary, "expiry", StringComparison.Ordinal) ? Now : Now.AddMinutes(5),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.RsaSha256));

        var logger = new TelegramOidcTestLogger<TelegramOidcTokenValidator>();
        string encodedToken = new JwtSecurityTokenHandler().WriteToken(token);
        Result<TelegramOidcIdentity> result = await CreateValidator(new StaticConfiguration(key), logger)
            .ValidateAsync(encodedToken, new TelegramOidcNonce("nonce"), CancellationToken.None);
        Assert.True(result.IsFailure);
        logger.AssertRejection("token_validation", reason, encodedToken, "https://attacker.invalid", "other");
    }

    [Fact]
    public async Task UnknownSigningKey_RejectsAndRequestsRefresh() {
        using var trusted = RSA.Create(2048);
        using var untrusted = RSA.Create(2048);
        var configuration = new StaticConfiguration(new RsaSecurityKey(trusted) { KeyId = "trusted" });
        var logger = new TelegramOidcTestLogger<TelegramOidcTokenValidator>();
        Result<TelegramOidcIdentity> result = await CreateValidator(configuration, logger).ValidateAsync(CreateToken(new RsaSecurityKey(untrusted) { KeyId = "untrusted" }), new TelegramOidcNonce("nonce"), CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.True(configuration.RefreshRequested);
        logger.AssertRejection("token_validation", "signing_key_not_found", "untrusted", "trusted");
    }

    [Fact]
    public async Task DisabledProvider_DoesNotFetchMetadata() {
        var configuration = new StaticConfiguration(signingKey: null);
        var logger = new TelegramOidcTestLogger<TelegramOidcTokenValidator>();
        var validator = new TelegramOidcTokenValidator(MsOptions.Create(new TelegramOidcOptions()), new FixedClock(), configuration, logger);
        Result<TelegramOidcIdentity> result = await validator.ValidateAsync("token", new TelegramOidcNonce("nonce"), CancellationToken.None);
        Assert.Equal("Authentication.TelegramOidcNotConfigured", result.Error.Code);
        Assert.Equal(0, configuration.FetchCount);
        logger.AssertRejection("token_validation", "not_configured");
    }

    [Fact]
    public async Task InvalidSignature_LogsOnlyFailureCategory() {
        using var trusted = RSA.Create(2048);
        using var untrusted = RSA.Create(2048);
        var logger = new TelegramOidcTestLogger<TelegramOidcTokenValidator>();
        string token = CreateToken(new RsaSecurityKey(untrusted) { KeyId = "same-key" });
        Result<TelegramOidcIdentity> result = await CreateValidator(new StaticConfiguration(new RsaSecurityKey(trusted) { KeyId = "same-key" }), logger)
            .ValidateAsync(token, new TelegramOidcNonce("nonce"), CancellationToken.None);
        Assert.True(result.IsFailure);
        logger.AssertRejection("token_validation", "signature_invalid", token, "same-key", "987654321");
    }

    private static TelegramOidcTokenValidator CreateValidator(StaticConfiguration configuration, ILogger<TelegramOidcTokenValidator>? logger = null) => new(
        MsOptions.Create(new TelegramOidcOptions {
            Enabled = true,
            ClientId = "123456",
            ClientSecret = "test-secret",
            RedirectUri = "https://app.example/callback",
        }), new FixedClock(), configuration, logger ?? NullLogger<TelegramOidcTokenValidator>.Instance);

    private static List<Claim> Claims() => [
        new("sub", "1234123412341234123"), new("id", "987654321", ClaimValueTypes.Integer64),
        new("nonce", "nonce"), new("given_name", "Alex"),
        new("iat", new DateTimeOffset(Now).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64),
    ];

    private static string CreateToken(SecurityKey key, List<Claim>? claims = null) =>
        new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            "https://oauth.telegram.org", "123456", claims ?? Claims(), Now.AddMinutes(-1), Now.AddMinutes(5),
            new SigningCredentials(key, SecurityAlgorithms.RsaSha256)));

    [ExcludeFromCodeCoverage]
    private sealed class FixedClock : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(Now);
    }

    [ExcludeFromCodeCoverage]
    private sealed class StaticConfiguration(SecurityKey? signingKey) : IConfigurationManager<OpenIdConnectConfiguration> {
        public Func<Task>? OnFetch { get; init; }
        public int FetchCount { get; private set; }
        public bool RefreshRequested { get; private set; }
        public async Task<OpenIdConnectConfiguration> GetConfigurationAsync(CancellationToken cancel) {
            FetchCount++;
            if (OnFetch is not null) {
                await OnFetch();
            }
            var configuration = new OpenIdConnectConfiguration();
            if (signingKey is not null) {
                configuration.SigningKeys.Add(signingKey);
            }
            return configuration;
        }
        public void RequestRefresh() => RefreshRequested = true;
    }
}
