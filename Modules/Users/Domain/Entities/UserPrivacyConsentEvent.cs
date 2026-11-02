using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Domain.Entities;

public sealed class UserPrivacyConsentEvent : Entity<Guid> {
    public UserId UserId { get; private set; }
    public PrivacyConsentPurpose Purpose { get; private set; }
    public bool Granted { get; private set; }
    public string NoticeVersion { get; private set; } = string.Empty;
    public DateTime OccurredAtUtc { get; private set; }

    private UserPrivacyConsentEvent() { }

    internal static UserPrivacyConsentEvent Create(UserId userId, PrivacyConsentPurpose purpose, bool granted, string version, DateTime nowUtc) {
        DomainGuard.Defined(purpose, nameof(purpose));
        if (userId == UserId.Empty || string.IsNullOrWhiteSpace(version) || version.Length > 64) {
            throw new ArgumentException("A user and bounded notice version are required.", nameof(version));
        }
        var item = new UserPrivacyConsentEvent {
            Id = Guid.NewGuid(),
            UserId = userId,
            Purpose = purpose,
            Granted = granted,
            NoticeVersion = version,
            OccurredAtUtc = DomainGuard.RequiredUtc(nowUtc, nameof(nowUtc)),
        };
        item.SetCreated(nowUtc);
        return item;
    }
}
