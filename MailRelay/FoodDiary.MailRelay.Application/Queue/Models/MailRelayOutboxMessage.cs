namespace FoodDiary.MailRelay.Application.Queue.Models;

public sealed record MailRelayOutboxMessage(
    Guid Id,
    Guid EmailId,
    int AttemptCount);
