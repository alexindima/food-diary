using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct FastingOccurrenceId(Guid Value) : IEntityId<Guid> {
    public static FastingOccurrenceId New() => new(Guid.NewGuid());
    public static FastingOccurrenceId Empty => new(Guid.Empty);

    public static implicit operator Guid(FastingOccurrenceId id) => id.Value;
    public static explicit operator FastingOccurrenceId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
