using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.ValueObjects.Ids;

public readonly record struct NotificationId(Guid Value) : IEntityId<Guid> {
    public static NotificationId New() => new(Guid.NewGuid());
    public static NotificationId Empty => new(Guid.Empty);

    public static implicit operator Guid(NotificationId id) => id.Value;
    public static explicit operator NotificationId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
