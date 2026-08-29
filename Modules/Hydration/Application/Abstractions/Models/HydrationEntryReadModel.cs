namespace FoodDiary.Application.Abstractions.Hydration.Models;

public sealed record HydrationEntryReadModel(Guid Id, DateTime Timestamp, int AmountMl);
