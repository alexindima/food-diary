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

    public static bool HasValidSecretKey(JwtOptions options) =>
        !string.IsNullOrWhiteSpace(options.SecretKey) && options.SecretKey.Length >= 32;

    public static bool HasValidIssuer(JwtOptions options) => !string.IsNullOrWhiteSpace(options.Issuer);

    public static bool HasValidAudience(JwtOptions options) => !string.IsNullOrWhiteSpace(options.Audience);

    public static bool HasValidExpirationMinutes(JwtOptions options) => options.ExpirationMinutes > 0;

    public static bool HasValidRefreshTokenExpirationDays(JwtOptions options) => options.RefreshTokenExpirationDays > 0;
}
