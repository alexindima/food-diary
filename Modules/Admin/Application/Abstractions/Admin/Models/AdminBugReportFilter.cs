namespace FoodDiary.Application.Abstractions.Admin.Models;

public sealed record AdminBugReportFilter(int Page, int Limit, DateTimeOffset? FromUtc, DateTimeOffset? ToUtc, string? Status, string? Search, Guid? Id);
