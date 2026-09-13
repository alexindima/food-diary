namespace FoodDiary.Modules.Admin.Presentation.Features.Admin.Responses;

public sealed record AdminBugReportPageHttpResponse(IReadOnlyList<AdminBugReportHttpResponse> Items, long TotalItems, bool IsConfigured);
