namespace FoodDiary.Modules.Admin.Presentation.Responses;

public sealed record AdminMailInboxDmarcReportHttpResponse(
    string? OrganizationName,
    string? ReportId,
    string? Domain,
    DateTimeOffset? DateRangeStartUtc,
    DateTimeOffset? DateRangeEndUtc,
    IReadOnlyList<AdminMailInboxDmarcRecordHttpResponse> Records);
