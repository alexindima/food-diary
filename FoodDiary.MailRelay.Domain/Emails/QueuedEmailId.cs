using FoodDiary.MailRelay.Domain.Common;

namespace FoodDiary.MailRelay.Domain.Emails;

public readonly record struct QueuedEmailId(Guid Value) : IEntityId<Guid> {
    public static QueuedEmailId New() => new(Guid.NewGuid());
    public static QueuedEmailId Empty => new(Guid.Empty);

    public static implicit operator Guid(QueuedEmailId id) => id.Value;
    public static explicit operator QueuedEmailId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
