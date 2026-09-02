using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities.Users;

public sealed partial class User {
    public void UpdateAdminSecurity(UserAdminSecurityUpdate update) {
        EnsureNotDeleted();

        UserSecurityState securityState = GetSecurityState();
        if (!update.IsEmailConfirmed.HasValue || securityState.IsEmailConfirmed == update.IsEmailConfirmed.Value) {
            return;
        }

        ApplySecurityState(securityState.AsEmailConfirmed(update.IsEmailConfirmed.Value));
        SetModified();
    }

    public void UpdateAdminPreferences(UserAdminPreferenceUpdate update) {
        EnsureNotDeleted();

        if (update.Language is null) {
            return;
        }

        EnsureLanguage(update.Language, nameof(update.Language));
        string normalizedLanguage = NormalizeOptionalLanguage(update.Language, nameof(update.Language));
        UserPreferenceState preferenceState = GetPreferenceState();
        if (string.Equals(preferenceState.Language, normalizedLanguage, StringComparison.Ordinal)) {
            return;
        }

        ApplyPreferenceState(preferenceState with { Language = normalizedLanguage });
        SetModified();
    }

    public void UpdateAdminAiQuota(UserAdminAiQuotaUpdate update) {
        EnsureNotDeleted();
        if (ApplyAiTokenLimitChanges(update.ToAiTokenLimitUpdate())) {
            SetModified();
        }
    }
}
