namespace FoodDiary.Application.Hydration.Models;

public sealed record HydrationEntryModel(
    Guid Id,
    DateTime TimestampUtc,
    int AmountMl);
