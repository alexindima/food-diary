using FoodDiary.Domain.Events;

namespace FoodDiary.Domain.Entities.Users;

public sealed partial class User {
    public void Deactivate() {
        EnsureNotDeleted();
        IsActive = false;
        SetModified();
    }

    public void Activate() {
        if (DeletedAt is not null) {
            throw new InvalidOperationException("Deleted user cannot be activated directly. Use Restore().");
        }

        IsActive = true;
        SetModified();
    }

    public void MarkDeleted(DateTime deletedAtUtc) {
        if (DeletedAt is not null && !IsActive) {
            return;
        }

        var normalizedDeletedAtUtc = NormalizeUtcTimestamp(deletedAtUtc, nameof(deletedAtUtc));

        DeletedAt = normalizedDeletedAtUtc;
        IsActive = false;
        RaiseDomainEvent(new UserDeletedDomainEvent(Id, normalizedDeletedAtUtc));
        SetModified();
    }

    public void Restore() {
        if (DeletedAt is null && IsActive) {
            return;
        }

        DeletedAt = null;
        IsActive = true;
        RaiseDomainEvent(new UserRestoredDomainEvent(Id));
        SetModified();
    }
}
