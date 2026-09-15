namespace FoodDiary.Modules.Hydration.Presentation.Requests;

public sealed record UpdateHydrationEntryHttpRequest(
    DateTime? TimestampUtc,
    int? AmountMl);
