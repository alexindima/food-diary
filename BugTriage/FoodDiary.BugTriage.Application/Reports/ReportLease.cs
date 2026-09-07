namespace FoodDiary.BugTriage.Application.Reports;

public sealed record ReportLease(Guid Id, Guid SourceMessageId, string Subject, string TextBody,
    Guid LeaseToken, DateTimeOffset LeaseExpiresAtUtc, int Attempt, DateTimeOffset ContentExpiresAtUtc);
