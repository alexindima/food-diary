using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct AiUsageId(Guid Value) : IEntityId<Guid> {
    public static AiUsageId New() => new(Guid.NewGuid());
    public static AiUsageId Empty => new(Guid.Empty);

    public static implicit operator Guid(AiUsageId id) => id.Value;
    public static explicit operator AiUsageId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
