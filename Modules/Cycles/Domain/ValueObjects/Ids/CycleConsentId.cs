using FoodDiary.Domain.Primitives;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct CycleConsentId(Guid Value) : IEntityId<Guid> {
    public static CycleConsentId New() => new(Guid.NewGuid());
    public static CycleConsentId Empty => new(Guid.Empty);

    public static implicit operator Guid(CycleConsentId id) => id.Value;
    public static explicit operator CycleConsentId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
