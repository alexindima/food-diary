namespace FoodDiary.MailRelay.Client.Models;

public sealed record OutgoingEmailJournalPage(IReadOnlyList<OutgoingEmailJournalEntry> Items, long TotalItems, IReadOnlyDictionary<string, long>? StatusCounts = null);
