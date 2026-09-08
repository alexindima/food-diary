namespace FoodDiary.MailInbox.Client.Models;

public sealed record InboundMailMessagePageResponse(IReadOnlyList<InboundMailMessageSummaryResponse> Items, long TotalItems);
