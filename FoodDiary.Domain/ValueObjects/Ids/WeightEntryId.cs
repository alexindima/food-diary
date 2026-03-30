using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct WeightEntryId(Guid Value) : IEntityId<Guid> {
    public static WeightEntryId New() => new(Guid.NewGuid());
    public static WeightEntryId Empty => new(Guid.Empty);

    public static implicit operator Guid(WeightEntryId id) => id.Value;
    public static explicit operator WeightEntryId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
