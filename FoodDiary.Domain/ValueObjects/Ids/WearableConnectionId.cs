using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct WearableConnectionId(Guid Value) : IEntityId<Guid> {
    public static WearableConnectionId New() => new(Guid.NewGuid());
    public static WearableConnectionId Empty => new(Guid.Empty);

    public static implicit operator Guid(WearableConnectionId id) => id.Value;
    public static explicit operator WearableConnectionId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
