namespace FoodDiary.Presentation.Api.Features.Hydration.Requests;

public sealed record CreateHydrationFromOperationHttpRequest(DateTime TimestampUtc, int AmountMl);
