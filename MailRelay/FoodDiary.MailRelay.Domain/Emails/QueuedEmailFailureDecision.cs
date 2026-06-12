namespace FoodDiary.MailRelay.Domain.Emails;

public sealed record QueuedEmailFailureDecision(
    QueuedEmailId Id,
    int AttemptCount,
    string Status,
    bool IsTerminalFailure,
    string Error);
