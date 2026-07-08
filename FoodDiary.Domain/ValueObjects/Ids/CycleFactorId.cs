using FoodDiary.Domain.Primitives;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct CycleFactorId(Guid Value) : IEntityId<Guid> {
    public static CycleFactorId New() => new(Guid.NewGuid());
    public static CycleFactorId Empty => new(Guid.Empty);

    public static implicit operator Guid(CycleFactorId id) => id.Value;
    public static explicit operator CycleFactorId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
