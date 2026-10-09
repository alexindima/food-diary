namespace FoodDiary.MailRelay.Application.Queue.Models;

public sealed record MailRelayOutboxMessage(
    MailRelayOutboxId Id,
    QueuedEmailId EmailId,
    int AttemptCount);
