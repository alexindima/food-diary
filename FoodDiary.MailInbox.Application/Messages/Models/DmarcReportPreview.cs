namespace FoodDiary.MailInbox.Application.Messages.Models;

public sealed record DmarcReportPreview(
    string? OrganizationName,
    string? ReportId,
    string? Domain,
    DateTimeOffset? DateRangeStartUtc,
    DateTimeOffset? DateRangeEndUtc,
    IReadOnlyList<DmarcReportRecordPreview> Records);

public sealed record DmarcReportRecordPreview(
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
