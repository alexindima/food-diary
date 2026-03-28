using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities.Users;

public sealed partial class User {
    public void ApplyAdminUpdate(UserAdminUpdate update) {
        if (update.IsActive.HasValue) {
            SetActive(update.IsActive.Value);
        }

        UpdateAdminAccount(update.Account);

        if (update.Roles is not null) {
            ReplaceRoles(update.Roles);
        }
    }

    public void UpdateAdminAccount(UserAdminAccountUpdate update) {
        EnsureNotDeleted();

        var changed = false;
        var credentialState = GetCredentialState();
        var profileState = GetProfileState();

        if (update.IsEmailConfirmed.HasValue && credentialState.IsEmailConfirmed != update.IsEmailConfirmed.Value) {
            credentialState = credentialState.AsEmailConfirmed(update.IsEmailConfirmed.Value);
            changed = true;
        }

        if (update.Language is not null) {
            EnsureLanguage(update.Language, nameof(update.Language));
            var normalizedLanguage = NormalizeOptionalLanguage(update.Language, nameof(update.Language));
            if (profileState.Language != normalizedLanguage) {
                profileState = profileState with { Language = normalizedLanguage };
                changed = true;
            }
        }

        changed |= ApplyAiTokenLimitChanges(new UserAiTokenLimitUpdate(
            InputLimit: update.AiInputTokenLimit,
            OutputLimit: update.AiOutputTokenLimit));

        if (!changed) {
            return;
        }

        ApplyCredentialState(credentialState);
        ApplyProfileState(profileState);
        SetModified();
    }
}
