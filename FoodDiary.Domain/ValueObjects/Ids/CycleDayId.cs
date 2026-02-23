using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct CycleDayId(Guid Value) : IEntityId<Guid> {
    public static CycleDayId New() => new(Guid.NewGuid());
    public static CycleDayId Empty => new(Guid.Empty);

    public static implicit operator Guid(CycleDayId id) => id.Value;
    public static explicit operator CycleDayId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
