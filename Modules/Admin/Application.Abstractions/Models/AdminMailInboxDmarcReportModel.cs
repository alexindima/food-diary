namespace FoodDiary.Modules.Admin.Application.Abstractions.Models;

public sealed record AdminMailInboxDmarcReportModel(
    string? OrganizationName,
    string? ReportId,
    string? Domain,
    DateTimeOffset? DateRangeStartUtc,
    DateTimeOffset? DateRangeEndUtc,
    IReadOnlyList<AdminMailInboxDmarcRecordModel> Records);
