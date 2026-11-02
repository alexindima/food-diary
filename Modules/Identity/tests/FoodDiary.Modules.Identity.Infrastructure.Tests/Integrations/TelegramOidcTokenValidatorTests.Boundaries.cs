using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Infrastructure.Providers.Authentication;
using FoodDiary.Results;
using FoodDiary.Testing.Assertions;
using Microsoft.IdentityModel.Tokens;

namespace FoodDiary.Modules.Identity.Infrastructure.Tests.Integrations;

public sealed partial class TelegramOidcTokenValidatorTests {
    [Theory]
    [InlineData("audience", "audience_count_invalid")]
    [InlineData("authorized-party", "authorized_party_duplicate")]
    public async Task AmbiguousAudienceOrAuthorizedParty_IsRejected(string fault, string reason) {
        using var rsa = RSA.Create(2048);
        var key = new RsaSecurityKey(rsa) { KeyId = "owned-test-key" };
        List<Claim> claims = Claims();
        if (string.Equals(fault, "authorized-party", StringComparison.Ordinal)) {
            claims.Add(new Claim("azp", "123456"));
            claims.Add(new Claim("azp", "123456"));
        }
        var token = new JwtSecurityToken("https://oauth.telegram.org", "123456", claims, Now.AddMinutes(-1), Now.AddMinutes(5),
            new SigningCredentials(key, SecurityAlgorithms.RsaSha256));
        if (string.Equals(fault, "audience", StringComparison.Ordinal)) {
            token.Payload[JwtRegisteredClaimNames.Aud] = new[] { "123456", "other" };
        }
        string encoded = new JwtSecurityTokenHandler().WriteToken(token);
        var logger = new TelegramOidcTestLogger<TelegramOidcTokenValidator>();

        Result<TelegramOidcIdentity> result = await CreateValidator(new StaticConfiguration(key), logger)
            .ValidateAsync(encoded, new TelegramOidcNonce("nonce"), CancellationToken.None);

        Assert.Equal(TelegramIdentityErrors.InvalidProof, ResultAssert.Failure(result));
        logger.AssertRejection("token_validation", reason, encoded);
    }

    [Theory]
    [InlineData("malformed")]
    [InlineData("no-expiry")]
    [InlineData("unsupported-algorithm")]
    public async Task InvalidTokenShapeLifetimeOrAlgorithm_FailsClosedWithoutLoggingTheProof(string fault) {
        using var rsa = RSA.Create(2048);
        var key = new RsaSecurityKey(rsa) { KeyId = "owned-test-key" };
        var token = new JwtSecurityToken("https://oauth.telegram.org", "123456", Claims(), notBefore: Now.AddMinutes(-1),
            expires: string.Equals(fault, "no-expiry", StringComparison.Ordinal) ? null : Now.AddMinutes(5),
            signingCredentials: new SigningCredentials(key, string.Equals(fault, "unsupported-algorithm", StringComparison.Ordinal) ? SecurityAlgorithms.RsaSha512 : SecurityAlgorithms.RsaSha256));
        string encoded = string.Equals(fault, "malformed", StringComparison.Ordinal) ? "synthetic-malformed-proof" : new JwtSecurityTokenHandler().WriteToken(token);
        var logger = new TelegramOidcTestLogger<TelegramOidcTokenValidator>();

        Result<TelegramOidcIdentity> result = await CreateValidator(new StaticConfiguration(key), logger)
            .ValidateAsync(encoded, new TelegramOidcNonce("nonce"), CancellationToken.None);

        Assert.Equal(TelegramIdentityErrors.InvalidProof, ResultAssert.Failure(result));
        TelegramOidcTestLogger<TelegramOidcTokenValidator>.Entry entry = Assert.Single(logger.Entries);
        Assert.Multiple(
            () => Assert.Equal("token_validation", entry.Fields["Stage"]),
            () => Assert.Null(entry.Exception),
            () => Assert.DoesNotContain(encoded, entry.Message, StringComparison.Ordinal));
    }
}
