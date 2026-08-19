using FoodDiary.Domain.Common;
using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities.Users;

public sealed partial class User {
    public void LinkGoogleIdentity(string issuer, string subject) {
        EnsureNotDeleted();
        string normalizedIssuer = DomainGuard.RequiredText(issuer, 200, nameof(issuer));
        string normalizedSubject = DomainGuard.RequiredText(subject, 255, nameof(subject));

        GoogleIssuer = normalizedIssuer;
        GoogleSubject = normalizedSubject;
        SetModified();
    }

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
        UserSecurityState currentState = GetSecurityState();
        UserSecurityState nextState = currentState.WithRefreshToken(normalizedRefreshToken, effectiveChangedAtUtc);
        if (nextState == currentState) {
            return;
        }

        ApplySecurityState(nextState);

        SetModified(LatestAuditTimestamp(effectiveChangedAtUtc));
    }

    public void RecordAuthenticationActivity(DateTime occurredAtUtc) {
        EnsureNotDeleted();
        DateTime normalizedOccurredAtUtc = NormalizeUtcTimestamp(occurredAtUtc, nameof(occurredAtUtc));
        UserSecurityState currentState = GetSecurityState();
        UserSecurityState nextState = currentState.WithAuthenticationActivity(normalizedOccurredAtUtc);
        if (nextState == currentState) {
            return;
        }

        ApplySecurityState(nextState);
        SetModified(LatestAuditTimestamp(normalizedOccurredAtUtc));
    }

    public void UpdatePassword(string hashedPassword) {
        EnsureNotDeleted();
        ApplySecurityState(GetSecurityState().WithPassword(NormalizeRequiredPasswordHash(hashedPassword)));
        SetModified();
    }

    public void RequirePasswordChange() {
        EnsureNotDeleted();
        ApplySecurityState(GetSecurityState().RequiringPasswordChange());
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
        EnsureIssuanceIsNotFuture(normalizedIssuedAtUtc, nameof(issue.IssuedAtUtc));
        EnsureIssuanceDoesNotRegress(normalizedIssuedAtUtc, EmailConfirmationSentAtUtc, nameof(issue.IssuedAtUtc));
        EnsureFutureUtc(normalizedExpiresAtUtc, nameof(issue.ExpiresAtUtc));
        EnsureExpiresAfterIssuance(normalizedExpiresAtUtc, normalizedIssuedAtUtc, nameof(issue.ExpiresAtUtc));
        UserSecurityState nextState = GetSecurityState().WithEmailConfirmationToken(normalizedTokenHash, normalizedExpiresAtUtc, normalizedIssuedAtUtc);
        ApplySecurityState(nextState);
        SetModified(normalizedIssuedAtUtc);
    }

    public void CompleteEmailVerification() {
        SetEmailConfirmed(isConfirmed: true);
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
        EnsureIssuanceIsNotFuture(normalizedIssuedAtUtc, nameof(issue.IssuedAtUtc));
        EnsureIssuanceDoesNotRegress(normalizedIssuedAtUtc, PasswordResetSentAtUtc, nameof(issue.IssuedAtUtc));
        EnsureFutureUtc(normalizedExpiresAtUtc, nameof(issue.ExpiresAtUtc));
        EnsureExpiresAfterIssuance(normalizedExpiresAtUtc, normalizedIssuedAtUtc, nameof(issue.ExpiresAtUtc));
        UserSecurityState nextState = GetSecurityState().WithPasswordResetToken(normalizedTokenHash, normalizedExpiresAtUtc, normalizedIssuedAtUtc);
        ApplySecurityState(nextState);
        SetModified(normalizedIssuedAtUtc);
    }

    private static void EnsureIssuanceIsNotFuture(DateTime issuedAtUtc, string paramName) {
        if (issuedAtUtc > DomainTime.UtcNow) {
            throw new ArgumentOutOfRangeException(paramName, "Issuance date cannot be in the future.");
        }
    }

    private static void EnsureIssuanceDoesNotRegress(DateTime issuedAtUtc, DateTime? previousIssuedAtUtc, string paramName) {
        if (previousIssuedAtUtc.HasValue && issuedAtUtc < previousIssuedAtUtc.Value) {
            throw new ArgumentOutOfRangeException(paramName, "Issuance date cannot be earlier than the previous issuance date.");
        }
    }
}
