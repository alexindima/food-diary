namespace FoodDiary.Modules.Hydration.Application.Models;

public sealed record HydrationOperationModel(Guid OperationId, Guid EntryId, DateTime TimestampUtc, int AmountMl);
