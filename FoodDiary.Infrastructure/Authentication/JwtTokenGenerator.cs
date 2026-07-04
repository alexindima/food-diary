using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FoodDiary.Infrastructure.Authentication;

public sealed class JwtTokenGenerator : IJwtTokenGenerator {
    private readonly TimeProvider _dateTimeProvider;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly SymmetricSecurityKey _signingKey;
    private readonly int _accessTokenExpirationMinutes;
    private readonly int _refreshTokenExpirationMinutes;
    private readonly int _rememberMeRefreshTokenExpirationMinutes;

    public JwtTokenGenerator(IOptions<JwtOptions> options, TimeProvider dateTimeProvider) {
        JwtOptions jwtOptions = options.Value;
        _dateTimeProvider = dateTimeProvider;
        string secretKey = jwtOptions.SecretKey;
        _issuer = jwtOptions.Issuer;
        _audience = jwtOptions.Audience;

        if (!JwtOptions.HasValidSecretKey(jwtOptions)) {
            throw new InvalidOperationException($"{JwtOptions.SectionName}:SecretKey is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_issuer)) {
            throw new InvalidOperationException($"{JwtOptions.SectionName}:Issuer is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_audience)) {
            throw new InvalidOperationException($"{JwtOptions.SectionName}:Audience is not configured.");
        }

        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        _accessTokenExpirationMinutes = jwtOptions.ExpirationMinutes > 0 ? jwtOptions.ExpirationMinutes : 60;
        int refreshDays = jwtOptions.RefreshTokenExpirationDays > 0 ? jwtOptions.RefreshTokenExpirationDays : 7;
        _refreshTokenExpirationMinutes = refreshDays * 1440;
        int rememberMeRefreshDays = jwtOptions.RememberMeRefreshTokenExpirationDays > 0
            ? jwtOptions.RememberMeRefreshTokenExpirationDays
            : 90;
        _rememberMeRefreshTokenExpirationMinutes = rememberMeRefreshDays * 1440;
    }

    public string GenerateAccessToken(UserId userId, string email, IReadOnlyCollection<string> roles) =>
        GenerateToken(userId, email, roles, _accessTokenExpirationMinutes, expiresAtUtc: null, impersonation: null);

    public string GenerateAccessToken(
        UserId userId,
        string email,
        IReadOnlyCollection<string> roles,
        DateTime? expiresAtUtc) =>
        GenerateToken(userId, email, roles, _accessTokenExpirationMinutes, expiresAtUtc, impersonation: null);

    public string GenerateAccessToken(
        UserId userId,
        string email,
        IReadOnlyCollection<string> roles,
        JwtImpersonationContext impersonation) =>
        GenerateToken(userId, email, roles, _accessTokenExpirationMinutes, expiresAtUtc: null, impersonation);

    public string GenerateRefreshToken(
        UserId userId,
        string email,
        IReadOnlyCollection<string> roles,
        bool rememberMe = false,
        Guid? refreshSessionId = null) =>
        GenerateToken(
            userId,
            email,
            roles,
            rememberMe ? _rememberMeRefreshTokenExpirationMinutes : _refreshTokenExpirationMinutes,
            expiresAtUtc: null,
            impersonation: null,
            rememberMe,
            refreshSessionId);

    public (UserId userId, string email, bool rememberMe, Guid? refreshSessionId)? ValidateToken(string token) {
        try {
            var tokenHandler = new JwtSecurityTokenHandler();
            tokenHandler.ValidateToken(token, new TokenValidationParameters {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _signingKey,
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
            }, out SecurityToken? validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            var userIdValue = Guid.Parse(jwtToken.Claims.First(x => string.Equals(x.Type, ClaimTypes.NameIdentifier, StringComparison.Ordinal)).Value);
            string email = jwtToken.Claims.First(x => string.Equals(x.Type, ClaimTypes.Email, StringComparison.Ordinal)).Value;
            bool rememberMe = jwtToken.Claims.Any(static x =>
                string.Equals(x.Type, JwtClaimNames.RememberMe, StringComparison.Ordinal) &&
                string.Equals(x.Value, "true", StringComparison.OrdinalIgnoreCase));
            string? refreshSessionIdClaim = jwtToken.Claims.FirstOrDefault(static x =>
                string.Equals(x.Type, JwtClaimNames.RefreshSessionId, StringComparison.Ordinal))?.Value;
            Guid? refreshSessionId = Guid.TryParse(refreshSessionIdClaim, out Guid parsedRefreshSessionId)
                ? parsedRefreshSessionId
                : null;

            return (new UserId(userIdValue), email, rememberMe, refreshSessionId);
        } catch (Exception ex) when (ex is SecurityTokenException or ArgumentException or FormatException or InvalidOperationException) {
            return null;
        }
    }

    private string GenerateToken(
        UserId userId,
        string email,
        IReadOnlyCollection<string> roles,
        int expirationMinutes,
        DateTime? expiresAtUtc,
        JwtImpersonationContext? impersonation,
        bool rememberMe = false,
        Guid? refreshSessionId = null) {
        var credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim> {
            new(ClaimTypes.NameIdentifier, userId.Value.ToString()),
            new(ClaimTypes.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        if (impersonation is not null) {
            claims.Add(new Claim(JwtImpersonationClaimNames.IsImpersonation, "true"));
            claims.Add(new Claim(JwtImpersonationClaimNames.ActorUserId, impersonation.ActorUserId.Value.ToString()));
            claims.Add(new Claim(JwtImpersonationClaimNames.Reason, impersonation.Reason));
        }

        if (rememberMe) {
            claims.Add(new Claim(JwtClaimNames.RememberMe, "true"));
        }

        if (refreshSessionId.HasValue) {
            claims.Add(new Claim(JwtClaimNames.RefreshSessionId, refreshSessionId.Value.ToString()));
        }

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        DateTime configuredExpiresAtUtc = _dateTimeProvider.GetUtcNow().UtcDateTime.AddMinutes(expirationMinutes);
        DateTime tokenExpiresAtUtc = expiresAtUtc < configuredExpiresAtUtc
            ? expiresAtUtc.Value.ToUniversalTime()
            : configuredExpiresAtUtc;

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: tokenExpiresAtUtc,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static class JwtClaimNames {
        public const string RememberMe = "remember_me";
        public const string RefreshSessionId = "refresh_session_id";
    }
}
