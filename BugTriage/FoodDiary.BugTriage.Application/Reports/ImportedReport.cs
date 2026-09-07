namespace FoodDiary.BugTriage.Application.Reports;

public sealed record ImportedReport(Guid SourceMessageId, DateTimeOffset ReceivedAtUtc,
    string Subject, string TextBody, byte[]? RawMime);
