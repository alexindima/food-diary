namespace FoodDiary.Presentation.Api.Features.Admin.Responses;

public sealed record AdminMailInboxMessagePageHttpResponse(IReadOnlyList<AdminMailInboxMessageSummaryHttpResponse> Items, long TotalItems, long? UnreadCount = null, long? ReadCount = null);
