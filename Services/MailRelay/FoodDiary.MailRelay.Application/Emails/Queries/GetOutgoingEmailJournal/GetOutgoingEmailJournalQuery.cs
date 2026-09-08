using FoodDiary.Mediator;

namespace FoodDiary.MailRelay.Application.Emails.Queries.GetOutgoingEmailJournal;

public sealed record GetOutgoingEmailJournalQuery(int Page, int Limit, string? Purpose, string? Status, string? Recipient, DateTimeOffset? FromUtc = null, DateTimeOffset? ToUtc = null, Guid? Id = null, string? CorrelationId = null) : IRequest<Result<OutgoingEmailJournalPage>>;
