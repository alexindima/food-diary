using FoodDiary.BugTriage.Application.Reports.Identifiers;
namespace FoodDiary.BugTriage.Application.Reports;

public sealed record ReportLease(BugReportId Id, SourceMessageId SourceMessageId, string Subject, string TextBody,
    LeaseToken LeaseToken, DateTimeOffset LeaseExpiresAtUtc, int Attempt, DateTimeOffset ContentExpiresAtUtc);
