namespace FoodDiary.Application.Hydration.Models;

public sealed record HydrationOperationModel(Guid OperationId, Guid EntryId, DateTime TimestampUtc, int AmountMl);
