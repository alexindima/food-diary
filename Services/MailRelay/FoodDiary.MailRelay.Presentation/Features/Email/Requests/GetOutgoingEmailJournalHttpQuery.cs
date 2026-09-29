using System.ComponentModel.DataAnnotations;

namespace FoodDiary.MailRelay.Presentation.Features.Email.Requests;

public sealed record GetOutgoingEmailJournalHttpQuery(
    [Range(1, 10_000)] int Page = 1,
    [Range(1, 100)] int Limit = 50,
    string? Purpose = null,
    string? Status = null,
    string? Recipient = null,
    DateTimeOffset? FromUtc = null,
    DateTimeOffset? ToUtc = null,
    Guid? Id = null,
    string? CorrelationId = null);
