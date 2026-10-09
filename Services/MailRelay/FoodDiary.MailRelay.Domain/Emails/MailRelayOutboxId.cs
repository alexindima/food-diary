using FoodDiary.Domain.Primitives;

namespace FoodDiary.MailRelay.Domain.Emails;

public readonly record struct MailRelayOutboxId(Guid Value) : IEntityId<Guid> {
    public override string ToString() => Value.ToString();
}
