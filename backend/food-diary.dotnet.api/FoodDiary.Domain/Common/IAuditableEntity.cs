namespace FoodDiary.Domain.Common;

/// <summary>
/// Интерфейс для сущностей с аудитом создания и изменения
/// </summary>
public interface IAuditableEntity
{
    /// <summary>
    /// Дата и время создания сущности (UTC)
    /// </summary>
    DateTime CreatedOnUtc { get; }

    /// <summary>
    /// Дата и время последнего изменения сущности (UTC)
    /// </summary>
    DateTime? ModifiedOnUtc { get; }
}
