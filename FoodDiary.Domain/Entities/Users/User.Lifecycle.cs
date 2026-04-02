using FoodDiary.Domain.Events;

namespace FoodDiary.Domain.Entities.Users;

public sealed partial class User {
    public void DeleteAccount(DateTime deletedAtUtc) {
        var normalizedDeletedAtUtc = NormalizeUtcTimestamp(deletedAtUtc, nameof(deletedAtUtc));
        UpdateRefreshToken(null, normalizedDeletedAtUtc);
        MarkDeleted(normalizedDeletedAtUtc);
    }

    public void Deactivate(DateTime? changedAtUtc = null) {
        EnsureNotDeleted();
        var effectiveChangedAtUtc = NormalizeOptionalAuditTimestamp(changedAtUtc, nameof(changedAtUtc));
        ApplyAccountState(GetAccountState().Deactivate());
        SetModified(effectiveChangedAtUtc);
    }

    public void Activate(DateTime? changedAtUtc = null) {
        if (DeletedAt is not null) {
            throw new InvalidOperationException("Deleted user cannot be activated directly. Use Restore().");
        }

        var effectiveChangedAtUtc = NormalizeOptionalAuditTimestamp(changedAtUtc, nameof(changedAtUtc));
        ApplyAccountState(GetAccountState().Activate());
        SetModified(effectiveChangedAtUtc);
    }

    public void MarkDeleted(DateTime deletedAtUtc) {
        if (DeletedAt is not null && !IsActive) {
            return;
        }

        var normalizedDeletedAtUtc = NormalizeUtcTimestamp(deletedAtUtc, nameof(deletedAtUtc));

        ApplyAccountState(GetAccountState().MarkDeleted(normalizedDeletedAtUtc));
        RaiseDomainEvent(new UserDeletedDomainEvent(Id, normalizedDeletedAtUtc, normalizedDeletedAtUtc));
        SetModified(normalizedDeletedAtUtc);
    }

    public void Restore(DateTime? restoredAtUtc = null) {
        if (DeletedAt is null && IsActive) {
            return;
        }

        var normalizedRestoredAtUtc = NormalizeOptionalAuditTimestamp(restoredAtUtc, nameof(restoredAtUtc));
        ApplyAccountState(GetAccountState().Restore());
        RaiseDomainEvent(new UserRestoredDomainEvent(Id, normalizedRestoredAtUtc));
        SetModified(normalizedRestoredAtUtc);
    }
}
