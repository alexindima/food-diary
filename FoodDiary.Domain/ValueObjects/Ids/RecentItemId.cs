using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct RecentItemId(Guid Value) : IEntityId<Guid> {
    public static RecentItemId New() => new(Guid.NewGuid());
    public static RecentItemId Empty => new(Guid.Empty);

    public static implicit operator Guid(RecentItemId id) => id.Value;
    public static explicit operator RecentItemId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
