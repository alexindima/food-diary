using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using FoodDiary.Application.Common.Interfaces.Authentication;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Infrastructure.Authentication;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IConfiguration configuration) => _configuration = configuration;

    public string GenerateAccessToken(UserId userId, string email) =>
        GenerateToken(userId, email, int.Parse(_configuration["JwtSettings:ExpirationMinutes"] ?? "60"));

    public string GenerateRefreshToken(UserId userId, string email) =>
        GenerateToken(userId, email, int.Parse(_configuration["JwtSettings:RefreshTokenExpirationDays"] ?? "7") * 1440);

    public (UserId userId, string email)? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"]!);

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["JwtSettings:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["JwtSettings:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out var validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            var userIdValue = Guid.Parse(jwtToken.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value);
            var email = jwtToken.Claims.First(x => x.Type == ClaimTypes.Email).Value;

            return (new UserId(userIdValue), email);
        }
        catch
        {
            return null;
        }
    }

    private string GenerateToken(UserId userId, string email, int expirationMinutes)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"],
            audience: _configuration["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
