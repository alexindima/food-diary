using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Users;

public sealed class UserRefreshTokenSession : Entity<Guid> {
    public UserId UserId { get; private set; }
    public string RefreshTokenHash { get; private set; } = string.Empty;
    public bool RememberMe { get; private set; }
    public string? AuthProvider { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime LastRotatedAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }

    private UserRefreshTokenSession() {
    }

    public static UserRefreshTokenSession Create(
        Guid id,
        UserId userId,
        string refreshTokenHash,
        bool rememberMe,
        string? authProvider,
        string? ipAddress,
        string? userAgent,
        DateTime nowUtc) {
        if (id == Guid.Empty) {
            throw new ArgumentException("Session id must not be empty.", nameof(id));
        }

        if (userId.Value == Guid.Empty) {
            throw new ArgumentException("User id must not be empty.", nameof(userId));
        }

        DateTime normalizedNow = NormalizeUtcTimestamp(nowUtc, nameof(nowUtc));
        var session = new UserRefreshTokenSession {
            Id = id,
            UserId = userId,
            RefreshTokenHash = NormalizeRequiredText(refreshTokenHash, nameof(refreshTokenHash), 512),
            RememberMe = rememberMe,
            AuthProvider = NormalizeOptionalText(authProvider, 64),
            IpAddress = NormalizeOptionalText(ipAddress, 128),
            UserAgent = NormalizeOptionalText(userAgent, 512),
            CreatedAtUtc = normalizedNow,
            LastRotatedAtUtc = normalizedNow,
        };
        session.SetCreated(normalizedNow);
        return session;
    }

    public bool IsActive => RevokedAtUtc is null;

    public void Rotate(string refreshTokenHash, bool rememberMe, DateTime nowUtc) {
        if (!IsActive) {
            throw new InvalidOperationException("Revoked refresh token session cannot be rotated.");
        }

        DateTime normalizedNow = NormalizeUtcTimestamp(nowUtc, nameof(nowUtc));
        RefreshTokenHash = NormalizeRequiredText(refreshTokenHash, nameof(refreshTokenHash), 512);
        RememberMe = rememberMe;
        LastRotatedAtUtc = normalizedNow;
        SetModified(normalizedNow);
    }

    public void Revoke(DateTime nowUtc) {
        if (!IsActive) {
            return;
        }

        DateTime normalizedNow = NormalizeUtcTimestamp(nowUtc, nameof(nowUtc));
        RevokedAtUtc = normalizedNow;
        SetModified(normalizedNow);
    }

    private static string NormalizeRequiredText(string value, string paramName, int maxLength) {
        if (string.IsNullOrWhiteSpace(value)) {
            throw new ArgumentException("Value is required.", paramName);
        }

        string trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static string? NormalizeOptionalText(string? value, int maxLength) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        string trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static DateTime NormalizeUtcTimestamp(DateTime value, string paramName) {
        return value.Kind == DateTimeKind.Unspecified ? throw new ArgumentOutOfRangeException(paramName, "UTC timestamp kind must be specified.") : value.ToUniversalTime();
    }
}
