using FoodDiary.Domain.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Tracking;

public sealed class CycleConsent : Entity<CycleConsentId> {
    public CycleProfileId CycleProfileId { get; private set; }
    public CycleConsentPurpose Purpose { get; private set; }
    public DateTime GrantedAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }

    public CycleProfile CycleProfile { get; private set; } = null!;
    public bool IsActive => RevokedAtUtc is null;

    private CycleConsent() {
    }

    private CycleConsent(CycleConsentId id) : base(id) {
    }

    internal static CycleConsent Create(
        CycleProfileId cycleProfileId,
        CycleConsentPurpose purpose,
        DateTime grantedAtUtc) {
        EnsureCycleProfileId(cycleProfileId);
        EnsurePurpose(purpose);

        var consent = new CycleConsent(CycleConsentId.New()) {
            CycleProfileId = cycleProfileId,
            Purpose = purpose,
            GrantedAtUtc = DomainGuard.RequiredUtc(grantedAtUtc, nameof(grantedAtUtc)),
        };
        consent.SetCreated();
        return consent;
    }

    internal bool Grant(DateTime grantedAtUtc) {
        DateTime normalized = DomainGuard.RequiredUtc(grantedAtUtc, nameof(grantedAtUtc));
        if (IsActive) {
            return false;
        }

        if (RevokedAtUtc is { } revokedAtUtc && normalized < revokedAtUtc) {
            throw new ArgumentOutOfRangeException(nameof(grantedAtUtc), "Consent grant cannot precede its revocation.");
        }

        GrantedAtUtc = normalized;
        RevokedAtUtc = null;
        SetModified();
        return true;
    }

    internal void Revoke(DateTime revokedAtUtc) {
        if (RevokedAtUtc is not null) {
            return;
        }

        DateTime normalized = DomainGuard.RequiredUtc(revokedAtUtc, nameof(revokedAtUtc));
        if (normalized < GrantedAtUtc) {
            throw new ArgumentOutOfRangeException(nameof(revokedAtUtc), "Revocation cannot precede consent grant.");
        }

        RevokedAtUtc = normalized;
        SetModified();
    }

    private static void EnsureCycleProfileId(CycleProfileId cycleProfileId) {
        if (cycleProfileId == CycleProfileId.Empty) {
            throw new ArgumentException("CycleProfileId is required.", nameof(cycleProfileId));
        }
    }

    private static void EnsurePurpose(CycleConsentPurpose purpose) {
        if (!Enum.IsDefined(purpose)) {
            throw new ArgumentOutOfRangeException(nameof(purpose), "Purpose must be supported.");
        }
    }
}
