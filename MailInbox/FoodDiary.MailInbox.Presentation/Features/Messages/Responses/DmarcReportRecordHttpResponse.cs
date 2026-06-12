namespace FoodDiary.MailInbox.Presentation.Features.Messages.Responses;

public sealed record DmarcReportRecordHttpResponse(
    string? SourceIp,
    int Count,
    string? Disposition,
    string? Dkim,
    string? Spf,
    string? HeaderFrom,
    string? EnvelopeFrom,
    string? DkimDomain,
    string? DkimResult,
    string? SpfDomain,
    string? SpfResult);
