using FoodDiary.Domain.Primitives;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct ClientTaskId(Guid Value) : IEntityId<Guid> {
    public static ClientTaskId New() => new(Guid.NewGuid());
    public static ClientTaskId Empty => new(Guid.Empty);

    public static implicit operator Guid(ClientTaskId id) => id.Value;
    public static explicit operator ClientTaskId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
