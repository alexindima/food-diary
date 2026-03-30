using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct HydrationEntryId(Guid Value) : IEntityId<Guid> {
    public static HydrationEntryId New() => new(Guid.NewGuid());
    public static HydrationEntryId Empty => new(Guid.Empty);

    public static implicit operator Guid(HydrationEntryId id) => id.Value;
    public static explicit operator HydrationEntryId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
