namespace FoodDiary.Application.Abstractions.Admin.Common;

public sealed record BugAcknowledgementCandidate(Guid InboxId, string Recipient, string? MessageId, string IdempotencyKey, string Locale);
