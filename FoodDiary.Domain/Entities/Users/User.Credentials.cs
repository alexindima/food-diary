using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities.Users;

public sealed partial class User {
    public void CompletePasswordReset(string hashedPassword) {
        EnsureNotDeleted();
        Password = NormalizeRequiredPasswordHash(hashedPassword);
        ApplyCredentialState(GetCredentialState().WithoutPasswordResetToken());
        SetModified();
    }

    public void UpdateRefreshToken(string? refreshToken, DateTime? changedAtUtc = null) {
        UpdateRefreshToken(new UserRefreshTokenUpdate(refreshToken, changedAtUtc));
    }

    public void UpdateRefreshToken(UserRefreshTokenUpdate update) {
        EnsureNotDeleted();
        var normalizedRefreshToken = NormalizeOptionalToken(update.RefreshToken);
        var effectiveChangedAtUtc = NormalizeOptionalAuditTimestamp(update.ChangedAtUtc, nameof(update.ChangedAtUtc));
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
        SetEmailConfirmationToken(new UserTokenIssue(tokenHash, expiresAtUtc, issuedAtUtc));
    }

    public void SetEmailConfirmationToken(UserTokenIssue issue) {
        EnsureNotDeleted();
        var normalizedTokenHash = NormalizeRequiredTokenHash(issue.TokenHash, nameof(issue.TokenHash));
        var normalizedExpiresAtUtc = NormalizeUtcTimestamp(issue.ExpiresAtUtc, nameof(issue.ExpiresAtUtc));
        var normalizedIssuedAtUtc = NormalizeOptionalAuditTimestamp(issue.IssuedAtUtc, nameof(issue.IssuedAtUtc));
        EnsureFutureUtc(normalizedExpiresAtUtc, nameof(issue.ExpiresAtUtc));
        var nextState = GetCredentialState().WithEmailConfirmationToken(normalizedTokenHash, normalizedExpiresAtUtc, normalizedIssuedAtUtc);
        ApplyCredentialState(nextState);
        SetModified(normalizedIssuedAtUtc);
    }

    public void CompleteEmailVerification() {
        SetEmailConfirmed(true);
    }

    public void SetEmailConfirmed(bool isConfirmed) {
        EnsureNotDeleted();
        ApplyCredentialState(GetCredentialState().AsEmailConfirmed(isConfirmed));
        SetModified();
    }

    public void SetPasswordResetToken(string tokenHash, DateTime expiresAtUtc, DateTime? issuedAtUtc = null) {
        SetPasswordResetToken(new UserTokenIssue(tokenHash, expiresAtUtc, issuedAtUtc));
    }

    public void SetPasswordResetToken(UserTokenIssue issue) {
        EnsureNotDeleted();
        var normalizedTokenHash = NormalizeRequiredTokenHash(issue.TokenHash, nameof(issue.TokenHash));
        var normalizedExpiresAtUtc = NormalizeUtcTimestamp(issue.ExpiresAtUtc, nameof(issue.ExpiresAtUtc));
        var normalizedIssuedAtUtc = NormalizeOptionalAuditTimestamp(issue.IssuedAtUtc, nameof(issue.IssuedAtUtc));
        EnsureFutureUtc(normalizedExpiresAtUtc, nameof(issue.ExpiresAtUtc));
        var nextState = GetCredentialState().WithPasswordResetToken(normalizedTokenHash, normalizedExpiresAtUtc, normalizedIssuedAtUtc);
        ApplyCredentialState(nextState);
        SetModified(normalizedIssuedAtUtc);
    }
}
