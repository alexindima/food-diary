namespace FoodDiary.MailInbox.Presentation.Features.Messages.Responses;

public sealed record DmarcReportHttpResponse(
    string? OrganizationName,
    string? ReportId,
    string? Domain,
    DateTimeOffset? DateRangeStartUtc,
    DateTimeOffset? DateRangeEndUtc,
    IReadOnlyList<DmarcReportRecordHttpResponse> Records);
