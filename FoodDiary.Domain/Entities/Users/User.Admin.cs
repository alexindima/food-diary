using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities.Users;

public sealed partial class User {
    public void UpdateAdminAccount(UserAdminAccountUpdate update) {
        EnsureNotDeleted();

        var changed = false;
        var securityState = GetSecurityState();
        var preferenceState = GetPreferenceState();

        if (update.IsEmailConfirmed.HasValue && securityState.IsEmailConfirmed != update.IsEmailConfirmed.Value) {
            securityState = securityState.AsEmailConfirmed(update.IsEmailConfirmed.Value);
            changed = true;
        }

        if (update.Language is not null) {
            EnsureLanguage(update.Language, nameof(update.Language));
            var normalizedLanguage = NormalizeOptionalLanguage(update.Language, nameof(update.Language));
            if (preferenceState.Language != normalizedLanguage) {
                preferenceState = preferenceState with { Language = normalizedLanguage };
                changed = true;
            }
        }

        changed |= ApplyAiTokenLimitChanges(new UserAiTokenLimitUpdate(
            InputLimit: update.AiInputTokenLimit,
            OutputLimit: update.AiOutputTokenLimit));

        if (!changed) {
            return;
        }

        ApplySecurityState(securityState);
        ApplyPreferenceState(preferenceState);
        SetModified();
    }
}
