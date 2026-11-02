using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.Privacy;

namespace FoodDiary.Modules.Users.Domain.Entities;

public sealed partial class User {
    private readonly List<UserPrivacyConsentEvent> _privacyConsentEvents = [];
    public IReadOnlyCollection<UserPrivacyConsentEvent> PrivacyConsentEvents => _privacyConsentEvents.AsReadOnly();
    public DateTime? HealthConsentAcceptedAtUtc { get; private set; }
    public DateTime? HealthConsentDeclinedAtUtc { get; private set; }
    public DateTime? MarketingConsentAcceptedAtUtc { get; private set; }
    public string? HealthConsentNoticeVersion { get; private set; }
    public string? AiConsentNoticeVersion { get; private set; }
    public string? MarketingConsentNoticeVersion { get; private set; }

    public bool HasCurrentAiConsent() => AiConsentAcceptedAt is not null
        && (string.Equals(AiConsentNoticeVersion, PrivacyNoticeVersions.Current, StringComparison.Ordinal)
            || (string.Equals(PrivacyNoticeVersions.Current, PrivacyNoticeVersions.LegacyCompatibleAiNotice, StringComparison.Ordinal)
                && (AiConsentNoticeVersion is null || string.Equals(AiConsentNoticeVersion, PrivacyNoticeVersions.LegacyUnversioned, StringComparison.Ordinal))));

    public void RecordPrivacyConsent(PrivacyConsentPurpose purpose, bool granted, string noticeVersion, DateTime nowUtc) {
        EnsureNotDeleted();
        if (granted && !string.Equals(noticeVersion, PrivacyNoticeVersions.Current, StringComparison.Ordinal)) {
            throw new ArgumentException("The current privacy notice must be acknowledged.", nameof(noticeVersion));
        }
        var item = UserPrivacyConsentEvent.Create(Id, purpose, granted, noticeVersion, nowUtc);
        switch (purpose) {
            case PrivacyConsentPurpose.HealthTracking:
                HealthConsentAcceptedAtUtc = granted ? nowUtc : null;
                HealthConsentDeclinedAtUtc = granted ? null : nowUtc;
                HealthConsentNoticeVersion = noticeVersion;
                break;
            case PrivacyConsentPurpose.AiProcessing:
                AiConsentAcceptedAt = granted ? nowUtc : null;
                AiConsentNoticeVersion = noticeVersion;
                break;
            case PrivacyConsentPurpose.MarketingAttribution:
                MarketingConsentAcceptedAtUtc = granted ? nowUtc : null;
                MarketingConsentNoticeVersion = noticeVersion;
                break;
            default: throw new ArgumentOutOfRangeException(nameof(purpose));
        }
        _privacyConsentEvents.Add(item);
        SetModified();
    }
}
