using FoodDiary.Domain.Primitives;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct CycleSymptomEntryId(Guid Value) : IEntityId<Guid> {
    public static CycleSymptomEntryId New() => new(Guid.NewGuid());
    public static CycleSymptomEntryId Empty => new(Guid.Empty);

    public static implicit operator Guid(CycleSymptomEntryId id) => id.Value;
    public static explicit operator CycleSymptomEntryId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
