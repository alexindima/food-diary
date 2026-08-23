using FoodDiary.Domain.Events;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities.Users;

public sealed partial class User {
    public void DeleteAccount(DateTime deletedAtUtc) {
        MarkDeleted(deletedAtUtc);
    }

    public void Deactivate(DateTime? changedAtUtc = null) {
        EnsureNotDeleted();
        DateTime effectiveChangedAtUtc = NormalizeOptionalAuditTimestamp(changedAtUtc, nameof(changedAtUtc));
        UserAccountState currentState = GetAccountState();
        UserAccountState nextState = currentState.Deactivate();
        if (nextState == currentState) {
            return;
        }

        ApplyAccountState(nextState);
        AdvanceSecurityVersion();
        SetModified(effectiveChangedAtUtc);
    }

    public void Activate(DateTime? changedAtUtc = null) {
        if (DeletedAt is not null) {
            throw new InvalidOperationException("Deleted user cannot be activated directly. Use Restore().");
        }

        DateTime effectiveChangedAtUtc = NormalizeOptionalAuditTimestamp(changedAtUtc, nameof(changedAtUtc));
        UserAccountState currentState = GetAccountState();
        UserAccountState nextState = currentState.Activate();
        if (nextState == currentState) {
            return;
        }

        ApplyAccountState(nextState);
        AdvanceSecurityVersion();
        SetModified(effectiveChangedAtUtc);
    }

    public void MarkDeleted(DateTime deletedAtUtc) {
        if (DeletedAt is not null && !IsActive) {
            return;
        }

        DateTime normalizedDeletedAtUtc = NormalizeUtcTimestamp(deletedAtUtc, nameof(deletedAtUtc));

        ApplySecurityState(GetSecurityState().WithoutTransientTokens());
        ApplyAccountState(GetAccountState().MarkDeleted(normalizedDeletedAtUtc));
        AdvanceSecurityVersion();
        RaiseDomainEvent(new UserDeletedDomainEvent(Id, normalizedDeletedAtUtc, normalizedDeletedAtUtc));
        SetModified(normalizedDeletedAtUtc);
    }

    public void Restore(DateTime? restoredAtUtc = null) {
        if (DeletedAt is null && IsActive) {
            return;
        }

        DateTime normalizedRestoredAtUtc = NormalizeOptionalAuditTimestamp(restoredAtUtc, nameof(restoredAtUtc));
        ApplyAccountState(GetAccountState().Restore());
        AdvanceSecurityVersion();
        RaiseDomainEvent(new UserRestoredDomainEvent(Id, normalizedRestoredAtUtc));
        SetModified(normalizedRestoredAtUtc);
    }
}
