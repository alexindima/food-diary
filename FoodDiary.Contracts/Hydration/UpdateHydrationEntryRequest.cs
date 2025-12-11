namespace FoodDiary.Contracts.Hydration;

public record UpdateHydrationEntryRequest(
    DateTime? TimestampUtc,
    int? AmountMl);
