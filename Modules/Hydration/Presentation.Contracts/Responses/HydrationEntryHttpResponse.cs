namespace FoodDiary.Modules.Hydration.Presentation.Contracts.Responses;

public sealed record HydrationEntryHttpResponse(
    Guid Id,
    DateTime TimestampUtc,
    int AmountMl);
