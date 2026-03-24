namespace FoodDiary.Presentation.Api.Features.WeightEntries.Responses;

public sealed record WeightEntryHttpResponse(
    Guid Id,
    Guid UserId,
    DateTime Date,
    double Weight);
