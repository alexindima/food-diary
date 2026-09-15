namespace FoodDiary.Modules.Hydration.Contracts.Models;

public sealed record HydrationEntryModel(
    Guid Id,
    DateTime TimestampUtc,
    int AmountMl);
