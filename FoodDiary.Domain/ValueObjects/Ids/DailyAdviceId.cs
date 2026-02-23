using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct DailyAdviceId(Guid Value) : IEntityId<Guid> {
    public static DailyAdviceId New() => new(Guid.NewGuid());
    public static DailyAdviceId Empty => new(Guid.Empty);

    public static implicit operator Guid(DailyAdviceId id) => id.Value;
    public static explicit operator DailyAdviceId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
