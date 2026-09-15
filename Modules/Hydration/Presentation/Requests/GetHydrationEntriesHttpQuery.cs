namespace FoodDiary.Modules.Hydration.Presentation.Requests;

public sealed record GetHydrationEntriesHttpQuery(
    DateTime? DateUtc = null);
