namespace FoodDiary.Presentation.Api.Features.Hydration.Requests;

public sealed record UpdateHydrationEntryHttpRequest(
    DateTime? TimestampUtc,
    int? AmountMl);
