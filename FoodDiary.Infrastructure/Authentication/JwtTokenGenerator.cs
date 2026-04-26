using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FoodDiary.Infrastructure.Authentication;

public class JwtTokenGenerator : IJwtTokenGenerator {
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly SymmetricSecurityKey _signingKey;
    private readonly int _accessTokenExpirationMinutes;
    private readonly int _refreshTokenExpirationMinutes;

    public JwtTokenGenerator(IOptions<JwtOptions> options, IDateTimeProvider dateTimeProvider) {
        var jwtOptions = options.Value;
        _dateTimeProvider = dateTimeProvider;
        var secretKey = jwtOptions.SecretKey;
        _issuer = jwtOptions.Issuer;
        _audience = jwtOptions.Audience;

        if (string.IsNullOrWhiteSpace(secretKey)) {
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
        var refreshDays = jwtOptions.RefreshTokenExpirationDays > 0 ? jwtOptions.RefreshTokenExpirationDays : 7;
        _refreshTokenExpirationMinutes = refreshDays * 1440;
    }

    public string GenerateAccessToken(UserId userId, string email, IReadOnlyCollection<string> roles) =>
        GenerateToken(userId, email, roles, _accessTokenExpirationMinutes);

    public string GenerateRefreshToken(UserId userId, string email, IReadOnlyCollection<string> roles) =>
        GenerateToken(userId, email, roles, _refreshTokenExpirationMinutes);

    public (UserId userId, string email)? ValidateToken(string token) {
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
                ClockSkew = TimeSpan.Zero
            }, out var validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            var userIdValue = Guid.Parse(jwtToken.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value);
            var email = jwtToken.Claims.First(x => x.Type == ClaimTypes.Email).Value;

            return (new UserId(userIdValue), email);
        } catch (Exception ex) when (ex is SecurityTokenException or ArgumentException or FormatException or InvalidOperationException) {
            return null;
        }
    }

    private string GenerateToken(UserId userId, string email, IReadOnlyCollection<string> roles, int expirationMinutes) {
        var credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim> {
            new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var role in roles) {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: _dateTimeProvider.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
