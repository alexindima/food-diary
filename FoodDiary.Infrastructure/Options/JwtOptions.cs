using System.ComponentModel.DataAnnotations;

namespace FoodDiary.Infrastructure.Options;

public sealed class JwtOptions {
    public const string SectionName = "JwtSettings";

    [Required]
    [MinLength(32)]
    public string SecretKey { get; init; } = string.Empty;

    [Required]
    public string Issuer { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int ExpirationMinutes { get; init; }

    [Range(1, int.MaxValue)]
    public int RefreshTokenExpirationDays { get; init; }
}
