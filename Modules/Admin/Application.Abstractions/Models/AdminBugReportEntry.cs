namespace FoodDiary.Modules.Admin.Application.Abstractions.Models;

public sealed record AdminBugReportEntry(Guid Id, Guid SourceMessageId, DateTimeOffset ReceivedAtUtc,
    string Subject, string Status, int Attempt, string? Summary, string? MergeRequestUrl, bool ContentExpired);
