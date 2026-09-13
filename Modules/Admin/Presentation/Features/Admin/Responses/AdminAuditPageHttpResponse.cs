namespace FoodDiary.Modules.Admin.Presentation.Features.Admin.Responses;

public sealed record AdminAuditPageHttpResponse(IReadOnlyList<AdminAuditEntryHttpResponse> Items, int TotalItems);
