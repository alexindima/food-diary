namespace FoodDiary.MailInbox.Client.Models;

public sealed record DmarcReportResponse(
    string? OrganizationName,
    string? ReportId,
    string? Domain,
    DateTimeOffset? DateRangeStartUtc,
    DateTimeOffset? DateRangeEndUtc,
    IReadOnlyList<DmarcReportRecordResponse> Records);
