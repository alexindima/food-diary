namespace FoodDiary.Presentation.Api.Features.Admin.Responses;

public sealed record AdminMailInboxMessageDetailsHttpResponse(
    Guid Id,
    string? MessageId,
    string? FromAddress,
    IReadOnlyList<string> ToRecipients,
    string? Subject,
    string? TextBody,
    string? HtmlBody,
    string? RawMime,
    string Category,
    string Status,
    DateTimeOffset? ReadAtUtc,
    DateTimeOffset ReceivedAtUtc,
    DateTimeOffset? ContentPurgedAtUtc,
    AdminMailInboxDmarcReportHttpResponse? DmarcReport = null,
    string? EnvelopeFromAddress = null,
    bool IsTrustedRelay = false,
    bool FromAddressIsVerified = false,
    bool DmarcReportIsVerified = false);
