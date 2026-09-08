namespace FoodDiary.MailRelay.Presentation.Features.Email.Requests;

public sealed record GetOutgoingEmailJournalHttpQuery(int Page = 1, int Limit = 50, string? Purpose = null, string? Status = null, string? Recipient = null, DateTimeOffset? FromUtc = null, DateTimeOffset? ToUtc = null, Guid? Id = null, string? CorrelationId = null);
