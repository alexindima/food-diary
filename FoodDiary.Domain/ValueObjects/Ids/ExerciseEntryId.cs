using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct ExerciseEntryId(Guid Value) : IEntityId<Guid> {
    public static ExerciseEntryId New() => new(Guid.NewGuid());
    public static ExerciseEntryId Empty => new(Guid.Empty);

    public static implicit operator Guid(ExerciseEntryId id) => id.Value;
    public static explicit operator ExerciseEntryId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
