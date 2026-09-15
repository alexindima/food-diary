namespace FoodDiary.Modules.Hydration.Application.Abstractions.Models;

public sealed record HydrationEntryReadModel(Guid Id, DateTime Timestamp, int AmountMl);
