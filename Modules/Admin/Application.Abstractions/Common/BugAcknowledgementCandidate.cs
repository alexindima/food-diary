namespace FoodDiary.Modules.Admin.Application.Abstractions.Common;

public sealed record BugAcknowledgementCandidate(Guid InboxId, string Recipient, string? MessageId, string IdempotencyKey, string Locale);
