namespace FoodDiary.Domain.Entities.Users;

public sealed partial class User {
    public void UpdateRefreshToken(string? refreshToken, DateTime? changedAtUtc = null) {
        EnsureNotDeleted();
        var normalizedRefreshToken = NormalizeOptionalToken(refreshToken);
        var effectiveChangedAtUtc = NormalizeOptionalAuditTimestamp(changedAtUtc, nameof(changedAtUtc));
        var nextState = GetCredentialState().WithRefreshToken(normalizedRefreshToken, effectiveChangedAtUtc);
        ApplyCredentialState(nextState);

        SetModified(effectiveChangedAtUtc);
    }

    public void UpdatePassword(string hashedPassword) {
        EnsureNotDeleted();
        Password = NormalizeRequiredPasswordHash(hashedPassword);
        SetModified();
    }

    public void SetEmailConfirmationToken(string tokenHash, DateTime expiresAtUtc, DateTime? issuedAtUtc = null) {
        EnsureNotDeleted();
        var normalizedTokenHash = NormalizeRequiredTokenHash(tokenHash, nameof(tokenHash));
        var normalizedExpiresAtUtc = NormalizeUtcTimestamp(expiresAtUtc, nameof(expiresAtUtc));
        var normalizedIssuedAtUtc = NormalizeOptionalAuditTimestamp(issuedAtUtc, nameof(issuedAtUtc));
        EnsureFutureUtc(normalizedExpiresAtUtc, nameof(expiresAtUtc));
        var nextState = GetCredentialState().WithEmailConfirmationToken(normalizedTokenHash, normalizedExpiresAtUtc, normalizedIssuedAtUtc);
        ApplyCredentialState(nextState);
        SetModified(normalizedIssuedAtUtc);
    }

    public void ConfirmEmail() {
        SetEmailConfirmed(true);
    }

    public void SetEmailConfirmed(bool isConfirmed) {
        EnsureNotDeleted();
        ApplyCredentialState(GetCredentialState().AsEmailConfirmed(isConfirmed));
        SetModified();
    }

    public void SetPasswordResetToken(string tokenHash, DateTime expiresAtUtc, DateTime? issuedAtUtc = null) {
        EnsureNotDeleted();
        var normalizedTokenHash = NormalizeRequiredTokenHash(tokenHash, nameof(tokenHash));
        var normalizedExpiresAtUtc = NormalizeUtcTimestamp(expiresAtUtc, nameof(expiresAtUtc));
        var normalizedIssuedAtUtc = NormalizeOptionalAuditTimestamp(issuedAtUtc, nameof(issuedAtUtc));
        EnsureFutureUtc(normalizedExpiresAtUtc, nameof(expiresAtUtc));
        var nextState = GetCredentialState().WithPasswordResetToken(normalizedTokenHash, normalizedExpiresAtUtc, normalizedIssuedAtUtc);
        ApplyCredentialState(nextState);
        SetModified(normalizedIssuedAtUtc);
    }

    public void ClearPasswordResetToken() {
        EnsureNotDeleted();
        ApplyCredentialState(GetCredentialState().WithoutPasswordResetToken());
        SetModified();
    }
}
