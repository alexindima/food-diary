namespace FoodDiary.MailInbox.Client.Models;

public sealed record DmarcReportRecordResponse(
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
