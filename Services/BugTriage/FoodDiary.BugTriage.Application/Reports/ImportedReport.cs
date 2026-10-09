using FoodDiary.BugTriage.Application.Reports.Identifiers;
namespace FoodDiary.BugTriage.Application.Reports;

public sealed record ImportedReport(SourceMessageId SourceMessageId, DateTimeOffset ReceivedAtUtc,
    string Subject, string TextBody, byte[]? RawMime);
