namespace FoodDiary.Modules.Admin.Presentation.Responses;

public sealed record AdminBugReportPageHttpResponse(IReadOnlyList<AdminBugReportHttpResponse> Items, long TotalItems, bool IsConfigured);
