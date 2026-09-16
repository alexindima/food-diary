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
        Result<TelegramOidcIdentity> result = await CreateValidator(configuration)
            .ValidateAsync(new string('x', tokenLength), new string('n', nonceLength), CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Equal(0, configuration.FetchCount);
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
        TelegramOidcTokenValidator validator = CreateValidator(configuration);
        if (callerCancelled) {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => validator.ValidateAsync("token", "nonce", cancellation.Token));
        } else {
            Assert.True((await validator.ValidateAsync("token", "nonce", cancellation.Token)).IsFailure);
        }
        Assert.Equal(1, configuration.FetchCount);
    }

    private static readonly DateTime Now = new(2026, 9, 12, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ValidToken_PreservesDistinctSubjectAndTelegramId() {
        using var rsa = RSA.Create(2048);
        var key = new RsaSecurityKey(rsa) { KeyId = "key" };
        TelegramOidcTokenValidator validator = CreateValidator(new StaticConfiguration(key));

        Result<TelegramOidcIdentity> result = await validator.ValidateAsync(CreateToken(key), "nonce", CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.Equal(987654321, result.Value.TelegramUserId);
        Assert.Equal("1234123412341234123", result.Value.Subject);
        Assert.Equal("Alex", result.Value.FirstName);
    }

    [Theory]
    [InlineData("nonce", "wrong")]
    [InlineData("sub", "")]
    [InlineData("id", "-1")]
    [InlineData("id", "not-a-number")]
    [InlineData("id", "9223372036854775808")]
    [InlineData("iat", "9999999999")]
    [InlineData("azp", "other-client")]
    public async Task InvalidClaim_IsRejected(string claimType, string value) {
        using var rsa = RSA.Create(2048);
        var key = new RsaSecurityKey(rsa) { KeyId = "key" };
        List<Claim> claims = Claims();
        claims.RemoveAll(claim => string.Equals(claim.Type, claimType, StringComparison.Ordinal));
        claims.Add(new Claim(claimType, value));

        Result<TelegramOidcIdentity> result = await CreateValidator(new StaticConfiguration(key))
            .ValidateAsync(CreateToken(key, claims), "nonce", CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Theory]
    [InlineData("nonce")]
    [InlineData("id")]
    [InlineData("sub")]
    [InlineData("iat")]
    public async Task MissingRequiredClaim_IsRejected(string claimType) {
        using var rsa = RSA.Create(2048);
        var key = new RsaSecurityKey(rsa) { KeyId = "key" };
        List<Claim> claims = Claims();
        claims.RemoveAll(claim => string.Equals(claim.Type, claimType, StringComparison.Ordinal));
        Result<TelegramOidcIdentity> result = await CreateValidator(new StaticConfiguration(key))
            .ValidateAsync(CreateToken(key, claims), "nonce", CancellationToken.None);
        Assert.True(result.IsFailure);
    }

    [Theory]
    [InlineData("issuer")]
    [InlineData("audience")]
    [InlineData("expiry")]
    [InlineData("not-before")]
    [InlineData("duplicate-nonce")]
    public async Task InvalidTokenBoundary_IsRejected(string boundary) {
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

        Result<TelegramOidcIdentity> result = await CreateValidator(new StaticConfiguration(key))
            .ValidateAsync(new JwtSecurityTokenHandler().WriteToken(token), "nonce", CancellationToken.None);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task UnknownSigningKey_RejectsAndRequestsRefresh() {
        using var trusted = RSA.Create(2048);
        using var untrusted = RSA.Create(2048);
        var configuration = new StaticConfiguration(new RsaSecurityKey(trusted) { KeyId = "trusted" });
        Result<TelegramOidcIdentity> result = await CreateValidator(configuration).ValidateAsync(
            CreateToken(new RsaSecurityKey(untrusted) { KeyId = "untrusted" }), "nonce", CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.True(configuration.RefreshRequested);
    }

    [Fact]
    public async Task DisabledProvider_DoesNotFetchMetadata() {
        var configuration = new StaticConfiguration(signingKey: null);
        var validator = new TelegramOidcTokenValidator(MsOptions.Create(new TelegramOidcOptions()), new FixedClock(), configuration);
        Result<TelegramOidcIdentity> result = await validator.ValidateAsync("token", "nonce", CancellationToken.None);
        Assert.Equal("Authentication.TelegramOidcNotConfigured", result.Error.Code);
        Assert.Equal(0, configuration.FetchCount);
    }

    private static TelegramOidcTokenValidator CreateValidator(StaticConfiguration configuration) => new(
        MsOptions.Create(new TelegramOidcOptions {
            Enabled = true,
            ClientId = "123456",
            ClientSecret = "test-secret",
            RedirectUri = "https://app.example/callback",
        }), new FixedClock(), configuration);

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
