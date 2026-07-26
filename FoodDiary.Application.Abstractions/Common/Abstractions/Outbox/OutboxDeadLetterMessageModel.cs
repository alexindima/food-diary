namespace FoodDiary.Application.Abstractions.Common.Abstractions.Outbox;

public sealed record OutboxDeadLetterMessageModel(
    string OutboxName,
    Guid MessageId,
    DateTime CreatedOnUtc,
    DateTime DeadLetteredOnUtc,
    int AttemptCount,
    string? LastError,
    string Summary);
