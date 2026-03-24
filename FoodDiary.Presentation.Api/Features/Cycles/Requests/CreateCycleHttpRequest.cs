namespace FoodDiary.Presentation.Api.Features.Cycles.Requests;

public sealed record CreateCycleHttpRequest(
    DateTime StartDate,
    int? AverageLength,
    int? LutealLength,
    string? Notes);
