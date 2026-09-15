namespace FoodDiary.Modules.Hydration.Presentation.Responses;

public sealed record HydrationOperationHttpResponse(Guid OperationId, Guid EntryId, DateTime TimestampUtc, int AmountMl);
