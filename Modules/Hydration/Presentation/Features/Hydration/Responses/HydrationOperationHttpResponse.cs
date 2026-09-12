namespace FoodDiary.Presentation.Api.Features.Hydration.Responses;

public sealed record HydrationOperationHttpResponse(Guid OperationId, Guid EntryId, DateTime TimestampUtc, int AmountMl);
