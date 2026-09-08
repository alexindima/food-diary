namespace FoodDiary.Application.Abstractions.Admin.Models;

public sealed record AdminMailInboxMessagePageModel(IReadOnlyList<AdminMailInboxMessageSummaryModel> Items, long TotalItems, long? UnreadCount = null, long? ReadCount = null);
