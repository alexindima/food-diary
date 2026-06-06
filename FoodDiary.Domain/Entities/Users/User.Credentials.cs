using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities.Users;

public sealed partial class User {
    public void CompletePasswordReset(string hashedPassword) {
        EnsureNotDeleted();
        UserSecurityState nextState = GetSecurityState()
            .WithPassword(NormalizeRequiredPasswordHash(hashedPassword))
            .WithoutPasswordResetToken();
        ApplySecurityState(nextState);
        SetModified();
    }

    public void UpdateRefreshToken(string? refreshToken, DateTime? changedAtUtc = null) {
        UpdateRefreshToken(new UserRefreshTokenUpdate(refreshToken, changedAtUtc));
    }

    public void UpdateRefreshToken(UserRefreshTokenUpdate update) {
        EnsureNotDeleted();
        string? normalizedRefreshToken = NormalizeOptionalToken(update.RefreshToken);
        DateTime effectiveChangedAtUtc = NormalizeOptionalAuditTimestamp(update.ChangedAtUtc, nameof(update.ChangedAtUtc));
        UserSecurityState nextState = GetSecurityState().WithRefreshToken(normalizedRefreshToken, effectiveChangedAtUtc);
        ApplySecurityState(nextState);

        SetModified(effectiveChangedAtUtc);
    }

    public void RecordAuthenticationActivity(DateTime occurredAtUtc) {
        EnsureNotDeleted();
        DateTime normalizedOccurredAtUtc = NormalizeUtcTimestamp(occurredAtUtc, nameof(occurredAtUtc));
        ApplySecurityState(GetSecurityState().WithAuthenticationActivity(normalizedOccurredAtUtc));
        SetModified(normalizedOccurredAtUtc);
    }

    public void UpdatePassword(string hashedPassword) {
        EnsureNotDeleted();
        ApplySecurityState(GetSecurityState().WithPassword(NormalizeRequiredPasswordHash(hashedPassword)));
        SetModified();
    }

    public void SetEmailConfirmationToken(string tokenHash, DateTime expiresAtUtc, DateTime? issuedAtUtc = null) {
        SetEmailConfirmationToken(new UserTokenIssue(tokenHash, expiresAtUtc, issuedAtUtc));
    }

    public void SetEmailConfirmationToken(UserTokenIssue issue) {
        EnsureNotDeleted();
        string normalizedTokenHash = NormalizeRequiredTokenHash(issue.TokenHash, nameof(issue.TokenHash));
        DateTime normalizedExpiresAtUtc = NormalizeUtcTimestamp(issue.ExpiresAtUtc, nameof(issue.ExpiresAtUtc));
        DateTime normalizedIssuedAtUtc = NormalizeOptionalAuditTimestamp(issue.IssuedAtUtc, nameof(issue.IssuedAtUtc));
        EnsureFutureUtc(normalizedExpiresAtUtc, nameof(issue.ExpiresAtUtc));
        UserSecurityState nextState = GetSecurityState().WithEmailConfirmationToken(normalizedTokenHash, normalizedExpiresAtUtc, normalizedIssuedAtUtc);
        ApplySecurityState(nextState);
        SetModified(normalizedIssuedAtUtc);
    }

    public void CompleteEmailVerification() {
        SetEmailConfirmed(true);
    }

    public void SetEmailConfirmed(bool isConfirmed) {
        EnsureNotDeleted();
        ApplySecurityState(GetSecurityState().AsEmailConfirmed(isConfirmed));
        SetModified();
    }

    public void SetPasswordResetToken(string tokenHash, DateTime expiresAtUtc, DateTime? issuedAtUtc = null) {
        SetPasswordResetToken(new UserTokenIssue(tokenHash, expiresAtUtc, issuedAtUtc));
    }

    public void SetPasswordResetToken(UserTokenIssue issue) {
        EnsureNotDeleted();
        string normalizedTokenHash = NormalizeRequiredTokenHash(issue.TokenHash, nameof(issue.TokenHash));
        DateTime normalizedExpiresAtUtc = NormalizeUtcTimestamp(issue.ExpiresAtUtc, nameof(issue.ExpiresAtUtc));
        DateTime normalizedIssuedAtUtc = NormalizeOptionalAuditTimestamp(issue.IssuedAtUtc, nameof(issue.IssuedAtUtc));
        EnsureFutureUtc(normalizedExpiresAtUtc, nameof(issue.ExpiresAtUtc));
        UserSecurityState nextState = GetSecurityState().WithPasswordResetToken(normalizedTokenHash, normalizedExpiresAtUtc, normalizedIssuedAtUtc);
        ApplySecurityState(nextState);
        SetModified(normalizedIssuedAtUtc);
    }
}
