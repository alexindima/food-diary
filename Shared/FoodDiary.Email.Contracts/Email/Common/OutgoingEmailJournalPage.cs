namespace FoodDiary.Email.Contracts.Email.Common;

public sealed record OutgoingEmailJournalPage(IReadOnlyList<OutgoingEmailJournalEntry> Items, long TotalItems, IReadOnlyDictionary<string, long>? StatusCounts = null);
