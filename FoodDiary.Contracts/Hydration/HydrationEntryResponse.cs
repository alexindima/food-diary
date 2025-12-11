namespace FoodDiary.Contracts.Hydration;

public record HydrationEntryResponse(
    Guid Id,
    DateTime TimestampUtc,
    int AmountMl);
