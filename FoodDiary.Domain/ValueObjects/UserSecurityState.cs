namespace FoodDiary.Domain.ValueObjects;

public readonly record struct UserSecurityState(
    string Password,
    bool HasPassword,
    bool MustChangePassword,
    bool IsEmailConfirmed,
    string? EmailConfirmationTokenHash,
    DateTime? EmailConfirmationTokenExpiresAtUtc,
    DateTime? EmailConfirmationSentAtUtc,
    string? PasswordResetTokenHash,
    DateTime? PasswordResetTokenExpiresAtUtc,
    DateTime? PasswordResetSentAtUtc,
    DateTime? LastLoginAtUtc) {
    public static UserSecurityState CreateInitial(string passwordHash, bool hasPassword = true) {
        return new UserSecurityState(
            Password: passwordHash,
            HasPassword: hasPassword,
            MustChangePassword: false,
            IsEmailConfirmed: false,
            EmailConfirmationTokenHash: null,
            EmailConfirmationTokenExpiresAtUtc: null,
            EmailConfirmationSentAtUtc: null,
            PasswordResetTokenHash: null,
            PasswordResetTokenExpiresAtUtc: null,
            PasswordResetSentAtUtc: null,
            LastLoginAtUtc: null);
    }

    public UserSecurityState WithPassword(string passwordHash) {
        return this with {
            Password = passwordHash,
            HasPassword = true,
            MustChangePassword = false,
        };
    }

    public UserSecurityState RequiringPasswordChange() {
        if (!HasPassword) {
            throw new InvalidOperationException("A password must be set before a password change can be required.");
        }

        return this with {
            MustChangePassword = true,
        };
    }

    public UserSecurityState WithAuthenticationActivity(DateTime nowUtc) {
        return this with {
            LastLoginAtUtc = LatestLogin(nowUtc),
        };
    }

    public UserSecurityState WithEmailConfirmationToken(string tokenHash, DateTime expiresAtUtc, DateTime nowUtc) {
        return this with {
            EmailConfirmationTokenHash = tokenHash,
            EmailConfirmationTokenExpiresAtUtc = expiresAtUtc,
            EmailConfirmationSentAtUtc = nowUtc,
        };
    }

    public UserSecurityState AsEmailConfirmed(bool isConfirmed) {
        return this with {
            IsEmailConfirmed = isConfirmed,
            EmailConfirmationTokenHash = null,
            EmailConfirmationTokenExpiresAtUtc = null,
            EmailConfirmationSentAtUtc = null,
        };
    }

    public UserSecurityState WithPasswordResetToken(string tokenHash, DateTime expiresAtUtc, DateTime nowUtc) {
        return this with {
            PasswordResetTokenHash = tokenHash,
            PasswordResetTokenExpiresAtUtc = expiresAtUtc,
            PasswordResetSentAtUtc = nowUtc,
        };
    }

    public UserSecurityState WithoutPasswordResetToken() {
        return this with {
            PasswordResetTokenHash = null,
            PasswordResetTokenExpiresAtUtc = null,
            PasswordResetSentAtUtc = null,
        };
    }

    public UserSecurityState WithoutTransientTokens() {
        return this with {
            EmailConfirmationTokenHash = null,
            EmailConfirmationTokenExpiresAtUtc = null,
            EmailConfirmationSentAtUtc = null,
            PasswordResetTokenHash = null,
            PasswordResetTokenExpiresAtUtc = null,
            PasswordResetSentAtUtc = null,
        };
    }

    private DateTime LatestLogin(DateTime candidateUtc) {
        return LastLoginAtUtc is { } lastLoginAtUtc && lastLoginAtUtc > candidateUtc
            ? lastLoginAtUtc
            : candidateUtc;
    }
}
