namespace FoodDiary.MailInbox.Application.Messages.Models;

public sealed record InboundMailMessagePage(IReadOnlyList<InboundMailMessageSummary> Items, long TotalItems, long? UnreadCount = null, long? ReadCount = null);
