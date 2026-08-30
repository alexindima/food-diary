using FoodDiary.Domain.Primitives;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct CyclePredictionRevisionId(Guid Value) : IEntityId<Guid> {
    public static CyclePredictionRevisionId New() => new(Guid.NewGuid());

    public static CyclePredictionRevisionId Empty => new(Guid.Empty);

    public static implicit operator Guid(CyclePredictionRevisionId id) => id.Value;
    public static explicit operator CyclePredictionRevisionId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
