namespace FoodDiary.Modules.Admin.Presentation.Features.Admin.Responses;

public sealed record AdminBugReportHttpResponse(Guid Id, Guid SourceMessageId, DateTimeOffset ReceivedAtUtc,
    string Subject, string Status, int Attempt, string? Summary, string? MergeRequestUrl, bool ContentExpired);
