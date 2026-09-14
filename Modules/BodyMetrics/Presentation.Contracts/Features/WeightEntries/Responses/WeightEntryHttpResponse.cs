namespace FoodDiary.Modules.BodyMetrics.Presentation.Contracts.Features.WeightEntries.Responses;

public sealed record WeightEntryHttpResponse(
    Guid Id,
    Guid UserId,
    DateTime Date,
    double WeightKg);
