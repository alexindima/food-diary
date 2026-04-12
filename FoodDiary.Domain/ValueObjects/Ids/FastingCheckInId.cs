using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct FastingCheckInId(Guid Value) : IEntityId<Guid> {
    public static FastingCheckInId New() => new(Guid.NewGuid());
    public static FastingCheckInId Empty => new(Guid.Empty);

    public static implicit operator Guid(FastingCheckInId id) => id.Value;
    public static explicit operator FastingCheckInId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
