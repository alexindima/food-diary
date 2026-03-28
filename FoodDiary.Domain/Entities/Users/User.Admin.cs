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

        if (update.AiInputTokenLimit.HasValue) {
            if (update.AiInputTokenLimit.Value < 0) {
                throw new ArgumentOutOfRangeException(nameof(update.AiInputTokenLimit), "Input limit must be non-negative.");
            }

            if (AiInputTokenLimit != update.AiInputTokenLimit.Value) {
                AiInputTokenLimit = update.AiInputTokenLimit.Value;
                changed = true;
            }
        }

        if (update.AiOutputTokenLimit.HasValue) {
            if (update.AiOutputTokenLimit.Value < 0) {
                throw new ArgumentOutOfRangeException(nameof(update.AiOutputTokenLimit), "Output limit must be non-negative.");
            }

            if (AiOutputTokenLimit != update.AiOutputTokenLimit.Value) {
                AiOutputTokenLimit = update.AiOutputTokenLimit.Value;
                changed = true;
            }
        }

        if (!changed) {
            return;
        }

        ApplyCredentialState(credentialState);
        ApplyProfileState(profileState);
        SetModified();
    }
}
