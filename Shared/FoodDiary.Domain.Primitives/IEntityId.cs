namespace FoodDiary.Domain.Primitives;

public interface IEntityId<out T> where T : notnull {
    T Value { get; }
}
