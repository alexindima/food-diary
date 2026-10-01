using FoodDiary.Domain.Primitives;

namespace FoodDiary.Modules.Cycles.Domain.ValueObjects.Ids;

public readonly record struct CycleDayNoteId(Guid Value) : IEntityId<Guid> {
    public static CycleDayNoteId New() => new(Guid.NewGuid());
    public static CycleDayNoteId Empty => new(Guid.Empty);

    public static implicit operator Guid(CycleDayNoteId id) => id.Value;
    public static explicit operator CycleDayNoteId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
