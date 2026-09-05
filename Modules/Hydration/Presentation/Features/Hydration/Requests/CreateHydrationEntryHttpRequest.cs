namespace FoodDiary.Presentation.Api.Features.Hydration.Requests;

public sealed record CreateHydrationEntryHttpRequest(
    DateTime TimestampUtc,
    int AmountMl);
