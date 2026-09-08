namespace FoodDiary.MailInbox.Presentation.Features.Messages.Responses;

public sealed record InboundMailMessagePageHttpResponse(IReadOnlyList<InboundMailMessageSummaryHttpResponse> Items, long TotalItems, long? UnreadCount = null, long? ReadCount = null);
