using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Notifications;

public sealed class WebPushSubscription : AggregateRoot<WebPushSubscriptionId> {
    private const int EndpointMaxLength = 2048;
    private const int KeyMaxLength = 512;
    private const int LocaleMaxLength = 16;
    private const int UserAgentMaxLength = 512;

    public UserId UserId { get; private set; }
    public string Endpoint { get; private set; } = string.Empty;
    public string P256Dh { get; private set; } = string.Empty;
    public string Auth { get; private set; } = string.Empty;
    public DateTime? ExpirationTimeUtc { get; private set; }
    public string? Locale { get; private set; }
    public string? UserAgent { get; private set; }

    public User User { get; private set; } = null!;

    private WebPushSubscription() {
    }

    public static WebPushSubscription Create(
        UserId userId,
        string endpoint,
        string p256Dh,
        string auth,
        DateTime? expirationTimeUtc = null,
        string? locale = null,
        string? userAgent = null) {
        EnsureUserId(userId);

        var subscription = new WebPushSubscription {
            Id = WebPushSubscriptionId.New(),
            UserId = userId,
            Endpoint = NormalizeRequired(endpoint, EndpointMaxLength, nameof(endpoint)),
            P256Dh = NormalizeRequired(p256Dh, KeyMaxLength, nameof(p256Dh)),
            Auth = NormalizeRequired(auth, KeyMaxLength, nameof(auth)),
            ExpirationTimeUtc = NormalizeUtc(expirationTimeUtc, nameof(expirationTimeUtc)),
            Locale = NormalizeOptional(locale, LocaleMaxLength, nameof(locale)),
            UserAgent = NormalizeOptional(userAgent, UserAgentMaxLength, nameof(userAgent)),
        };

        subscription.SetCreated();
        return subscription;
    }

    public void Refresh(
        UserId userId,
        string p256Dh,
        string auth,
        DateTime? expirationTimeUtc = null,
        string? locale = null,
        string? userAgent = null) {
        EnsureUserId(userId);

        UserId = userId;
        P256Dh = NormalizeRequired(p256Dh, KeyMaxLength, nameof(p256Dh));
        Auth = NormalizeRequired(auth, KeyMaxLength, nameof(auth));
        ExpirationTimeUtc = NormalizeUtc(expirationTimeUtc, nameof(expirationTimeUtc));
        Locale = NormalizeOptional(locale, LocaleMaxLength, nameof(locale));
        UserAgent = NormalizeOptional(userAgent, UserAgentMaxLength, nameof(userAgent));
        SetModified();
    }

    private static void EnsureUserId(UserId userId) {
        if (userId == UserId.Empty) {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }
    }

    private static string NormalizeRequired(string value, int maxLength, string paramName) {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized)) {
            throw new ArgumentException("Value is required.", paramName);
        }

        return normalized.Length > maxLength
            ? throw new ArgumentOutOfRangeException(paramName, $"Value must be at most {maxLength} characters.")
            : normalized;
    }

    private static string? NormalizeOptional(string? value, int maxLength, string paramName) {
        if (value?.Trim() is not { Length: > 0 } normalized) {
            return null;
        }

        return normalized.Length > maxLength
            ? throw new ArgumentOutOfRangeException(paramName, $"Value must be at most {maxLength} characters.")
            : normalized;
    }

    private static DateTime? NormalizeUtc(DateTime? value, string paramName) {
        if (!value.HasValue) {
            return null;
        }

        if (value.Value.Kind == DateTimeKind.Unspecified) {
            throw new ArgumentOutOfRangeException(paramName, "UTC timestamp kind must be specified.");
        }

        return value.Value.ToUniversalTime();
    }
}
