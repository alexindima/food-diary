namespace FoodDiary.MailRelay.Services;

public sealed record MailRelayOutboxMessage(
    Guid Id,
    Guid EmailId,
    int AttemptCount);
