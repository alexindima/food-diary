namespace FoodDiary.Presentation.Api.Features.Admin.Responses;

public sealed record AdminBugReportPageHttpResponse(IReadOnlyList<AdminBugReportHttpResponse> Items, long TotalItems, bool IsConfigured);
