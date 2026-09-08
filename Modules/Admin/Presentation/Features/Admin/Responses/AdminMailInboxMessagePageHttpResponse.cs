namespace FoodDiary.Presentation.Api.Features.Admin.Responses;

public sealed record AdminMailInboxMessagePageHttpResponse(IReadOnlyList<AdminMailInboxMessageSummaryHttpResponse> Items, long TotalItems);
