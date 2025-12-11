namespace FoodDiary.Contracts.Hydration;

public record CreateHydrationEntryRequest(
    DateTime TimestampUtc,
    int AmountMl);
