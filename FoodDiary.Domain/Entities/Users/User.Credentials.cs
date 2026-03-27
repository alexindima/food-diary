namespace FoodDiary.Domain.Entities.Users;

public sealed partial class User {
    public void UpdateRefreshToken(string? refreshToken) {
        EnsureNotDeleted();
        var normalizedRefreshToken = NormalizeOptionalToken(refreshToken);
        var nextState = GetCredentialState().WithRefreshToken(normalizedRefreshToken, DateTime.UtcNow);
        ApplyCredentialState(nextState);

        SetModified();
    }

    public void UpdatePassword(string hashedPassword) {
        EnsureNotDeleted();
        Password = NormalizeRequiredPasswordHash(hashedPassword);
        SetModified();
    }

    public void SetEmailConfirmationToken(string tokenHash, DateTime expiresAtUtc) {
        EnsureNotDeleted();
        var normalizedTokenHash = NormalizeRequiredTokenHash(tokenHash, nameof(tokenHash));
        var normalizedExpiresAtUtc = NormalizeUtcTimestamp(expiresAtUtc, nameof(expiresAtUtc));
        EnsureFutureUtc(normalizedExpiresAtUtc, nameof(expiresAtUtc));
        var nextState = GetCredentialState().WithEmailConfirmationToken(normalizedTokenHash, normalizedExpiresAtUtc, DateTime.UtcNow);
        ApplyCredentialState(nextState);
        SetModified();
    }

    public void ConfirmEmail() {
        SetEmailConfirmed(true);
    }

    public void SetEmailConfirmed(bool isConfirmed) {
        EnsureNotDeleted();
        ApplyCredentialState(GetCredentialState().AsEmailConfirmed(isConfirmed));
        SetModified();
    }

    public void SetPasswordResetToken(string tokenHash, DateTime expiresAtUtc) {
        EnsureNotDeleted();
        var normalizedTokenHash = NormalizeRequiredTokenHash(tokenHash, nameof(tokenHash));
        var normalizedExpiresAtUtc = NormalizeUtcTimestamp(expiresAtUtc, nameof(expiresAtUtc));
        EnsureFutureUtc(normalizedExpiresAtUtc, nameof(expiresAtUtc));
        var nextState = GetCredentialState().WithPasswordResetToken(normalizedTokenHash, normalizedExpiresAtUtc, DateTime.UtcNow);
        ApplyCredentialState(nextState);
        SetModified();
    }

    public void ClearPasswordResetToken() {
        EnsureNotDeleted();
        ApplyCredentialState(GetCredentialState().WithoutPasswordResetToken());
        SetModified();
    }
}
