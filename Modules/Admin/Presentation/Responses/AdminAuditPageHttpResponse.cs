namespace FoodDiary.Modules.Admin.Presentation.Responses;

public sealed record AdminAuditPageHttpResponse(IReadOnlyList<AdminAuditEntryHttpResponse> Items, int TotalItems);
