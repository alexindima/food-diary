using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct CycleProfileId(Guid Value) : IEntityId<Guid> {
    public static CycleProfileId New() => new(Guid.NewGuid());
    public static CycleProfileId Empty => new(Guid.Empty);

    public static implicit operator Guid(CycleProfileId id) => id.Value;
    public static explicit operator CycleProfileId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
