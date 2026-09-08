namespace FoodDiary.Presentation.Api.Features.Admin.Responses;

public sealed record AdminAuditPageHttpResponse(IReadOnlyList<AdminAuditEntryHttpResponse> Items, int TotalItems);
