using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct BleedingEntryId(Guid Value) : IEntityId<Guid> {
    public static BleedingEntryId New() => new(Guid.NewGuid());
    public static BleedingEntryId Empty => new(Guid.Empty);

    public static implicit operator Guid(BleedingEntryId id) => id.Value;
    public static explicit operator BleedingEntryId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
