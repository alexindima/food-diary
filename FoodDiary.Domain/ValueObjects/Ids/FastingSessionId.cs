using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct FastingSessionId(Guid Value) : IEntityId<Guid> {
    public static FastingSessionId New() => new(Guid.NewGuid());
    public static FastingSessionId Empty => new(Guid.Empty);

    public static implicit operator Guid(FastingSessionId id) => id.Value;
    public static explicit operator FastingSessionId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
