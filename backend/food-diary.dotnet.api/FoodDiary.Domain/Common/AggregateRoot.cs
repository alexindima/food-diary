namespace FoodDiary.Domain.Common;

/// <summary>
/// Базовый класс для корней агрегатов в DDD
/// Агрегат - это кластер связанных объектов, которые рассматриваются как единое целое
/// </summary>
/// <typeparam name="TId">Тип идентификатора агрегата</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = new();

    /// <summary>
    /// Коллекция доменных событий, произошедших в агрегате
    /// </summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected AggregateRoot()
    {
    }

    protected AggregateRoot(TId id) : base(id)
    {
    }

    /// <summary>
    /// Регистрирует доменное событие
    /// </summary>
    /// <param name="domainEvent">Доменное событие</param>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Очищает список доменных событий (обычно вызывается после их обработки)
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
