namespace FoodDiary.Domain.Common;

/// <summary>
/// Маркерный интерфейс для доменных событий
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Дата и время возникновения события (UTC)
    /// </summary>
    DateTime OccurredOnUtc { get; }
}
