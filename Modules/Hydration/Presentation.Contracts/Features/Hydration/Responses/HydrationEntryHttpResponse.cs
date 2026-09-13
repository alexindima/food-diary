namespace FoodDiary.Presentation.Api.Features.Hydration.Responses;

public sealed record HydrationEntryHttpResponse(
    Guid Id,
    DateTime TimestampUtc,
    int AmountMl);
