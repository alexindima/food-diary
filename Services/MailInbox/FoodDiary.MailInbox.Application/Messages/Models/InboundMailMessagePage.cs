namespace FoodDiary.MailInbox.Application.Messages.Models;

public sealed record InboundMailMessagePage(IReadOnlyList<InboundMailMessageSummary> Items, long TotalItems);
