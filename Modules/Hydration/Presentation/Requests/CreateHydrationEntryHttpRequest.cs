namespace FoodDiary.Modules.Hydration.Presentation.Requests;

public sealed record CreateHydrationEntryHttpRequest(
    DateTime TimestampUtc,
    int AmountMl);
