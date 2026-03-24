namespace FoodDiary.Presentation.Api.Features.Cycles.Responses;

public sealed record CycleHttpResponse(
    Guid Id,
    Guid UserId,
    DateTime StartDate,
    int AverageLength,
    int LutealLength,
    string? Notes,
    IReadOnlyCollection<CycleDayHttpResponse> Days,
    CyclePredictionsHttpResponse? Predictions);
