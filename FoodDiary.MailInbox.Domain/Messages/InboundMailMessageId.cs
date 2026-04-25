using FoodDiary.MailInbox.Domain.Common;

namespace FoodDiary.MailInbox.Domain.Messages;

public readonly record struct InboundMailMessageId(Guid Value) : IEntityId<Guid> {
    public static readonly InboundMailMessageId Empty = new(Guid.Empty);

    public static InboundMailMessageId New() => new(Guid.NewGuid());

    public static explicit operator InboundMailMessageId(Guid value) => new(value);

    public static implicit operator Guid(InboundMailMessageId id) => id.Value;
}
