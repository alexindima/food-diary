namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct ClientTaskId(Guid Value) {
    public static ClientTaskId New() => new(Guid.NewGuid());

    public static ClientTaskId Empty => new(Guid.Empty);
}
