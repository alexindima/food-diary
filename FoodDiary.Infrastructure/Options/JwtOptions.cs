using System.ComponentModel.DataAnnotations;

namespace FoodDiary.Infrastructure.Options;

public sealed class JwtOptions {
    public const string SectionName = "Jwt";

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

    [Range(1, int.MaxValue)]
    public int RememberMeRefreshTokenExpirationDays { get; init; }

    public static bool HasValidSecretKey(JwtOptions options) {
        var value = options.SecretKey.Trim();
        return value.Length >= 32 &&
               !value.Equals("change-me-via-user-secrets-or-env-32", StringComparison.OrdinalIgnoreCase) &&
               !value.Equals("change-me-local-jwt-secret-min-32", StringComparison.OrdinalIgnoreCase) &&
               !value.Equals("your-32-character-or-longer-secret-key", StringComparison.OrdinalIgnoreCase);
    }

    public static bool HasValidIssuer(JwtOptions options) => !string.IsNullOrWhiteSpace(options.Issuer);

    public static bool HasValidAudience(JwtOptions options) => !string.IsNullOrWhiteSpace(options.Audience);

    public static bool HasValidExpirationMinutes(JwtOptions options) => options.ExpirationMinutes > 0;

    public static bool HasValidRefreshTokenExpirationDays(JwtOptions options) => options.RefreshTokenExpirationDays > 0;

    public static bool HasValidRememberMeRefreshTokenExpirationDays(JwtOptions options) =>
        options.RememberMeRefreshTokenExpirationDays > 0;
}
