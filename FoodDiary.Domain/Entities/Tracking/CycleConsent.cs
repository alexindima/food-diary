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
            GrantedAtUtc = NormalizeUtc(grantedAtUtc),
        };
        consent.SetCreated();
        return consent;
    }

    internal void Grant(DateTime grantedAtUtc) {
        GrantedAtUtc = NormalizeUtc(grantedAtUtc);
        RevokedAtUtc = null;
        SetModified();
    }

    internal void Revoke(DateTime revokedAtUtc) {
        if (RevokedAtUtc is not null) {
            return;
        }

        DateTime normalized = NormalizeUtc(revokedAtUtc);
        if (normalized < GrantedAtUtc) {
            throw new ArgumentOutOfRangeException(nameof(revokedAtUtc), "Revocation cannot precede consent grant.");
        }

        RevokedAtUtc = normalized;
        SetModified();
    }

    private static DateTime NormalizeUtc(DateTime value) =>
        value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime();

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
