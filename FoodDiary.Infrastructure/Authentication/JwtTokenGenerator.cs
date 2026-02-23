using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using FoodDiary.Application.Authentication.Abstractions;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Infrastructure.Authentication;

public class JwtTokenGenerator : IJwtTokenGenerator {
    private readonly string _issuer;
    private readonly string _audience;
    private readonly SymmetricSecurityKey _signingKey;
    private readonly int _accessTokenExpirationMinutes;
    private readonly int _refreshTokenExpirationMinutes;

    public JwtTokenGenerator(IConfiguration configuration) {
        var secretKey = configuration["JwtSettings:SecretKey"];
        _issuer = configuration["JwtSettings:Issuer"] ?? string.Empty;
        _audience = configuration["JwtSettings:Audience"] ?? string.Empty;

        if (string.IsNullOrWhiteSpace(secretKey)) {
            throw new InvalidOperationException("JwtSettings:SecretKey is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_issuer)) {
            throw new InvalidOperationException("JwtSettings:Issuer is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_audience)) {
            throw new InvalidOperationException("JwtSettings:Audience is not configured.");
        }

        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        _accessTokenExpirationMinutes = ParsePositiveInt(configuration["JwtSettings:ExpirationMinutes"], 60);
        var refreshDays = ParsePositiveInt(configuration["JwtSettings:RefreshTokenExpirationDays"], 7);
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
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static int ParsePositiveInt(string? rawValue, int fallback) {
        if (int.TryParse(rawValue, out var parsed) && parsed > 0) {
            return parsed;
        }

        return fallback;
    }
}
