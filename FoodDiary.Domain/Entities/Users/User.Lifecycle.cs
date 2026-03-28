using FoodDiary.Domain.Events;

namespace FoodDiary.Domain.Entities.Users;

public sealed partial class User {
    public void DeleteAccount(DateTime deletedAtUtc) {
        var normalizedDeletedAtUtc = NormalizeUtcTimestamp(deletedAtUtc, nameof(deletedAtUtc));
        UpdateRefreshToken(null, normalizedDeletedAtUtc);
        MarkDeleted(normalizedDeletedAtUtc);
    }

    public void SetActive(bool isActive, DateTime? changedAtUtc = null) {
        if (isActive) {
            Activate(changedAtUtc);
            return;
        }

        Deactivate(changedAtUtc);
    }

    public void Deactivate(DateTime? changedAtUtc = null) {
        EnsureNotDeleted();
        var effectiveChangedAtUtc = NormalizeOptionalAuditTimestamp(changedAtUtc, nameof(changedAtUtc));
        IsActive = false;
        SetModified(effectiveChangedAtUtc);
    }

    public void Activate(DateTime? changedAtUtc = null) {
        if (DeletedAt is not null) {
            throw new InvalidOperationException("Deleted user cannot be activated directly. Use Restore().");
        }

        var effectiveChangedAtUtc = NormalizeOptionalAuditTimestamp(changedAtUtc, nameof(changedAtUtc));
        IsActive = true;
        SetModified(effectiveChangedAtUtc);
    }

    public void MarkDeleted(DateTime deletedAtUtc) {
        if (DeletedAt is not null && !IsActive) {
            return;
        }

        var normalizedDeletedAtUtc = NormalizeUtcTimestamp(deletedAtUtc, nameof(deletedAtUtc));

        DeletedAt = normalizedDeletedAtUtc;
        IsActive = false;
        RaiseDomainEvent(new UserDeletedDomainEvent(Id, normalizedDeletedAtUtc, normalizedDeletedAtUtc));
        SetModified(normalizedDeletedAtUtc);
    }

    public void Restore(DateTime? restoredAtUtc = null) {
        if (DeletedAt is null && IsActive) {
            return;
        }

        var normalizedRestoredAtUtc = NormalizeOptionalAuditTimestamp(restoredAtUtc, nameof(restoredAtUtc));
        DeletedAt = null;
        IsActive = true;
        RaiseDomainEvent(new UserRestoredDomainEvent(Id, normalizedRestoredAtUtc));
        SetModified(normalizedRestoredAtUtc);
    }
}
