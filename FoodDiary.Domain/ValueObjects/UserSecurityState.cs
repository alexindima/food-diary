namespace FoodDiary.Domain.ValueObjects;

public readonly record struct UserSecurityState(
    string Password,
    string? RefreshToken,
    bool IsEmailConfirmed,
    string? EmailConfirmationTokenHash,
    DateTime? EmailConfirmationTokenExpiresAtUtc,
    DateTime? EmailConfirmationSentAtUtc,
    string? PasswordResetTokenHash,
    DateTime? PasswordResetTokenExpiresAtUtc,
    DateTime? PasswordResetSentAtUtc,
    DateTime? LastLoginAtUtc) {
    public static UserSecurityState CreateInitial(string passwordHash) {
        return new UserSecurityState(
            Password: passwordHash,
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

    public UserSecurityState WithPassword(string passwordHash) {
        return this with { Password = passwordHash };
    }

    public UserSecurityState WithRefreshToken(string? refreshToken, DateTime nowUtc) {
        return this with {
            RefreshToken = refreshToken,
            LastLoginAtUtc = refreshToken is null ? LastLoginAtUtc : nowUtc
        };
    }

    public UserSecurityState WithEmailConfirmationToken(string tokenHash, DateTime expiresAtUtc, DateTime nowUtc) {
        return this with {
            EmailConfirmationTokenHash = tokenHash,
            EmailConfirmationTokenExpiresAtUtc = expiresAtUtc,
            EmailConfirmationSentAtUtc = nowUtc
        };
    }

    public UserSecurityState AsEmailConfirmed(bool isConfirmed) {
        return this with {
            IsEmailConfirmed = isConfirmed,
            EmailConfirmationTokenHash = null,
            EmailConfirmationTokenExpiresAtUtc = null,
            EmailConfirmationSentAtUtc = null
        };
    }

    public UserSecurityState WithPasswordResetToken(string tokenHash, DateTime expiresAtUtc, DateTime nowUtc) {
        return this with {
            PasswordResetTokenHash = tokenHash,
            PasswordResetTokenExpiresAtUtc = expiresAtUtc,
            PasswordResetSentAtUtc = nowUtc
        };
    }

    public UserSecurityState WithoutPasswordResetToken() {
        return this with {
            PasswordResetTokenHash = null,
            PasswordResetTokenExpiresAtUtc = null,
            PasswordResetSentAtUtc = null
        };
    }

    public UserSecurityState WithoutTransientTokens() {
        return this with {
            RefreshToken = null,
            EmailConfirmationTokenHash = null,
            EmailConfirmationTokenExpiresAtUtc = null,
            EmailConfirmationSentAtUtc = null,
            PasswordResetTokenHash = null,
            PasswordResetTokenExpiresAtUtc = null,
            PasswordResetSentAtUtc = null
        };
    }
}
