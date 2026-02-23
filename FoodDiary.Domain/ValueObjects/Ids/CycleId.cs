using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct CycleId(Guid Value) : IEntityId<Guid> {
    public static CycleId New() => new(Guid.NewGuid());
    public static CycleId Empty => new(Guid.Empty);

    public static implicit operator Guid(CycleId id) => id.Value;
    public static explicit operator CycleId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
