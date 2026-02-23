namespace FoodDiary.Domain.Common;

public interface IEntityId<out T> where T : notnull {
    T Value { get; }
}
