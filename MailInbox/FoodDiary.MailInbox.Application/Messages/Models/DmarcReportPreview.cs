namespace FoodDiary.MailInbox.Application.Messages.Models;

public sealed record DmarcReportPreview(
    string? OrganizationName,
    string? ReportId,
    string? Domain,
    DateTimeOffset? DateRangeStartUtc,
    DateTimeOffset? DateRangeEndUtc,
    IReadOnlyList<DmarcReportRecordPreview> Records);
