using System.ComponentModel.DataAnnotations;

namespace FoodDiary.MailInbox.Presentation.Features.Messages.Requests;

public sealed record GetInboundMailMessagePageHttpQuery(
    [Range(1, 10_000)] int Page = 1,
    [Range(1, 100)] int Limit = 50,
    string? Recipient = null,
    string? Category = null,
    bool? Unread = null,
    DateTimeOffset? FromUtc = null,
    DateTimeOffset? ToUtc = null,
    string? Search = null,
    string? FromAddress = null,
    Guid? Id = null);
