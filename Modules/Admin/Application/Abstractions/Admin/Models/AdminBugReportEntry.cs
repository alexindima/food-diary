namespace FoodDiary.Application.Abstractions.Admin.Models;

public sealed record AdminBugReportEntry(Guid Id, Guid SourceMessageId, DateTimeOffset ReceivedAtUtc,
    string Subject, string Status, int Attempt, string? Summary, string? MergeRequestUrl, bool ContentExpired);
