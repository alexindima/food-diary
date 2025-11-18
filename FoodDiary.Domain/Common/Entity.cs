namespace FoodDiary.Domain.Common;

/// <summary>
/// Базовый класс для всех сущностей домена с поддержкой аудита
/// </summary>
/// <typeparam name="TId">Тип идентификатора сущности</typeparam>
public abstract class Entity<TId> : IAuditableEntity, IEquatable<Entity<TId>>
    where TId : notnull
{
    /// <summary>
    /// Уникальный идентификатор сущности
    /// </summary>
    public TId Id { get; protected set; } = default!;

    /// <summary>
    /// Дата и время создания сущности (UTC)
    /// </summary>
    public DateTime CreatedOnUtc { get; private set; }

    /// <summary>
    /// Дата и время последнего изменения сущности (UTC)
    /// </summary>
    public DateTime? ModifiedOnUtc { get; private set; }

    protected Entity()
    {
    }

    protected Entity(TId id)
    {
        Id = id;
    }

    /// <summary>
    /// Устанавливает время создания сущности
    /// </summary>
    protected void SetCreated()
    {
        CreatedOnUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Устанавливает время последнего изменения сущности
    /// </summary>
    protected void SetModified()
    {
        ModifiedOnUtc = DateTime.UtcNow;
    }

    #region Equality

    public bool Equals(Entity<TId>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((Entity<TId>)obj);
    }

    public override int GetHashCode()
    {
        return EqualityComparer<TId>.Default.GetHashCode(Id);
    }

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
    {
        return !Equals(left, right);
    }

    #endregion
}
