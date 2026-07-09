using System.Globalization;
using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Tracking;

public sealed class MarketingAttributionEvent : Entity<MarketingAttributionEventId> {
    private const int EventTypeMaxLength = 32;
    private const int AnonymousIdMaxLength = 96;
    private const int SessionIdMaxLength = 96;
    private const int LandingPathMaxLength = 512;
    private const int ReferrerHostMaxLength = 128;
    private const int UtmValueMaxLength = 160;
    private const int BuildVersionMaxLength = 64;

    public DateTime OccurredAtUtc { get; private set; }
    public Guid? UserId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string AnonymousId { get; private set; } = string.Empty;
    public string SessionId { get; private set; } = string.Empty;
    public string LandingPath { get; private set; } = string.Empty;
    public string? ReferrerHost { get; private set; }
    public string? UtmSource { get; private set; }
    public string? UtmMedium { get; private set; }
    public string? UtmCampaign { get; private set; }
    public string? UtmContent { get; private set; }
    public string? UtmTerm { get; private set; }
    public string? BuildVersion { get; private set; }

    private MarketingAttributionEvent() {
    }

    public static MarketingAttributionEvent Create(
        string eventType,
        DateTime occurredAtUtc,
        Guid? userId,
        string anonymousId,
        string sessionId,
        string landingPath,
        string? referrerHost = null,
        string? utmSource = null,
        string? utmMedium = null,
        string? utmCampaign = null,
        string? utmContent = null,
        string? utmTerm = null,
        string? buildVersion = null) {
        var entity = new MarketingAttributionEvent {
            Id = MarketingAttributionEventId.New(),
            OccurredAtUtc = NormalizeUtc(occurredAtUtc, nameof(occurredAtUtc)),
            UserId = userId is Guid value && value != Guid.Empty ? value : null,
            EventType = NormalizeRequired(eventType, EventTypeMaxLength, nameof(eventType)),
            AnonymousId = NormalizeRequired(anonymousId, AnonymousIdMaxLength, nameof(anonymousId)),
            SessionId = NormalizeRequired(sessionId, SessionIdMaxLength, nameof(sessionId)),
            LandingPath = NormalizeRequired(landingPath, LandingPathMaxLength, nameof(landingPath)),
            ReferrerHost = NormalizeOptional(referrerHost, ReferrerHostMaxLength),
            UtmSource = NormalizeOptional(utmSource, UtmValueMaxLength),
            UtmMedium = NormalizeOptional(utmMedium, UtmValueMaxLength),
            UtmCampaign = NormalizeOptional(utmCampaign, UtmValueMaxLength),
            UtmContent = NormalizeOptional(utmContent, UtmValueMaxLength),
            UtmTerm = NormalizeOptional(utmTerm, UtmValueMaxLength),
            BuildVersion = NormalizeOptional(buildVersion, BuildVersionMaxLength),
        };

        entity.SetCreated(entity.OccurredAtUtc);
        return entity;
    }

    private static string NormalizeRequired(string value, int maxLength, string paramName) {
        string? normalized = NormalizeOptional(value, maxLength);
        return normalized ?? throw new ArgumentException("Value is required.", paramName);
    }

    private static string? NormalizeOptional(string? value, int maxLength) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        string normalized = value.Trim();
        if (normalized.Length > maxLength) {
            throw new ArgumentOutOfRangeException(nameof(value), string.Create(CultureInfo.InvariantCulture, $"Value must be at most {maxLength} characters."));
        }

        return normalized;
    }

    private static DateTime NormalizeUtc(DateTime value, string paramName) {
        return value.Kind == DateTimeKind.Unspecified ? throw new ArgumentOutOfRangeException(paramName, "UTC timestamp kind must be specified.") : value.ToUniversalTime();
    }
}
