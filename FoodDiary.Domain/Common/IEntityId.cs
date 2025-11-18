namespace FoodDiary.Domain.Common;

/// <summary>
/// Базовый интерфейс для строготипизированных идентификаторов сущностей
/// </summary>
/// <typeparam name="T">Тип значения идентификатора (обычно Guid)</typeparam>
public interface IEntityId<out T> where T : notnull
{
    T Value { get; }
}
