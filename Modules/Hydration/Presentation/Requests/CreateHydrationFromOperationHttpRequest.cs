namespace FoodDiary.Modules.Hydration.Presentation.Requests;

public sealed record CreateHydrationFromOperationHttpRequest(DateTime TimestampUtc, int AmountMl);
