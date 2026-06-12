using FoodDiary.MailInbox.Domain.Common;
using FoodDiary.MailInbox.Domain.Messages;

namespace FoodDiary.MailInbox.Domain.Events;

public sealed record InboundMailMessageReceivedDomainEvent(
    InboundMailMessageId MessageId,
    DateTime? OccurredOnUtcOverride = null) : IDomainEvent {
    public DateTime OccurredOnUtc { get; } = OccurredOnUtcOverride?.ToUniversalTime() ?? DomainTime.UtcNow;
}
