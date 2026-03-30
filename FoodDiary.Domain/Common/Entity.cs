using System.Runtime.CompilerServices;

namespace FoodDiary.Domain.Common;

public abstract class Entity<TId> : IAuditableEntity, IEquatable<Entity<TId>>
    where TId : notnull {
    private TId _id = default!;
    private int? _cachedHashCode;

    public TId Id {
        get => _id;
        protected set {
            _id = value;
            _cachedHashCode = IsTransient()
                ? null
                : HashCode.Combine(GetType(), EqualityComparer<TId>.Default.GetHashCode(value));
        }
    }

    public DateTime CreatedOnUtc { get; private set; }

    public DateTime? ModifiedOnUtc { get; private set; }

    protected Entity() {
    }

    protected Entity(TId id) {
        Id = id;
    }

    protected void SetCreated() {
        SetCreated(DomainTime.UtcNow);
    }

    protected void SetCreated(DateTime createdOnUtc) {
        CreatedOnUtc = NormalizeUtc(createdOnUtc, nameof(createdOnUtc));
    }

    protected void SetModified() {
        SetModified(DomainTime.UtcNow);
    }

    protected void SetModified(DateTime modifiedOnUtc) {
        ModifiedOnUtc = NormalizeUtc(modifiedOnUtc, nameof(modifiedOnUtc));
    }

    private static DateTime NormalizeUtc(DateTime value, string paramName) {
        if (value.Kind == DateTimeKind.Unspecified) {
            throw new ArgumentOutOfRangeException(paramName, "UTC timestamp kind must be specified.");
        }

        return value.ToUniversalTime();
    }

    #region Equality
    public bool Equals(Entity<TId>? other) {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (other.GetType() != GetType()) return false;
        if (IsTransient() || other.IsTransient()) return false;
        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override bool Equals(object? obj) {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((Entity<TId>)obj);
    }

    // ReSharper disable NonReadonlyMemberInGetHashCode
    public override int GetHashCode() {
        if (_cachedHashCode.HasValue) {
            return _cachedHashCode.Value;
        }

        if (IsTransient()) {
            return RuntimeHelpers.GetHashCode(this);
        }

        _cachedHashCode = HashCode.Combine(GetType(), EqualityComparer<TId>.Default.GetHashCode(_id));
        return _cachedHashCode.Value;
    }

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) {
        return Equals(left, right);
    }

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) {
        return !Equals(left, right);
    }

    private bool IsTransient() {
        return EqualityComparer<TId>.Default.Equals(Id, default!);
    }

    #endregion
}
