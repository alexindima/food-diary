using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct FertilitySignalId(Guid Value) : IEntityId<Guid> {
    public static FertilitySignalId New() => new(Guid.NewGuid());
    public static FertilitySignalId Empty => new(Guid.Empty);

    public static implicit operator Guid(FertilitySignalId id) => id.Value;
    public static explicit operator FertilitySignalId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
