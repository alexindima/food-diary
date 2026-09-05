namespace FoodDiary.Presentation.Api.Features.Hydration.Requests;

public sealed record GetHydrationEntriesHttpQuery(
    DateTime? DateUtc = null);
