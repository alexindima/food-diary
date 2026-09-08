namespace FoodDiary.Application.Abstractions.Email.Common;

public sealed record OutgoingEmailJournalPage(IReadOnlyList<OutgoingEmailJournalEntry> Items, long TotalItems, IReadOnlyDictionary<string, long>? StatusCounts = null);
