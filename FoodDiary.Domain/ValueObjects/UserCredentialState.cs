namespace FoodDiary.Domain.ValueObjects;

public readonly record struct UserCredentialState(
    string? RefreshToken,
    bool IsEmailConfirmed,
    string? EmailConfirmationTokenHash,
    DateTime? EmailConfirmationTokenExpiresAtUtc,
    DateTime? EmailConfirmationSentAtUtc,
    string? PasswordResetTokenHash,
    DateTime? PasswordResetTokenExpiresAtUtc,
    DateTime? PasswordResetSentAtUtc,
    DateTime? LastLoginAtUtc) {
    public static UserCredentialState CreateInitial() {
        return new UserCredentialState(
            RefreshToken: null,
            IsEmailConfirmed: false,
            EmailConfirmationTokenHash: null,
            EmailConfirmationTokenExpiresAtUtc: null,
            EmailConfirmationSentAtUtc: null,
            PasswordResetTokenHash: null,
            PasswordResetTokenExpiresAtUtc: null,
            PasswordResetSentAtUtc: null,
            LastLoginAtUtc: null);
    }

    public UserCredentialState WithRefreshToken(string? refreshToken, DateTime nowUtc) {
        return this with {
            RefreshToken = refreshToken,
            LastLoginAtUtc = refreshToken is null ? LastLoginAtUtc : nowUtc
        };
    }

    public UserCredentialState WithEmailConfirmationToken(string tokenHash, DateTime expiresAtUtc, DateTime nowUtc) {
        return this with {
            EmailConfirmationTokenHash = tokenHash,
            EmailConfirmationTokenExpiresAtUtc = expiresAtUtc,
            EmailConfirmationSentAtUtc = nowUtc
        };
    }

    public UserCredentialState AsEmailConfirmed(bool isConfirmed) {
        return this with {
            IsEmailConfirmed = isConfirmed,
            EmailConfirmationTokenHash = null,
            EmailConfirmationTokenExpiresAtUtc = null,
            EmailConfirmationSentAtUtc = null
        };
    }

    public UserCredentialState WithPasswordResetToken(string tokenHash, DateTime expiresAtUtc, DateTime nowUtc) {
        return this with {
            PasswordResetTokenHash = tokenHash,
            PasswordResetTokenExpiresAtUtc = expiresAtUtc,
            PasswordResetSentAtUtc = nowUtc
        };
    }

    public UserCredentialState WithoutPasswordResetToken() {
        return this with {
            PasswordResetTokenHash = null,
            PasswordResetTokenExpiresAtUtc = null,
            PasswordResetSentAtUtc = null
        };
    }
}
