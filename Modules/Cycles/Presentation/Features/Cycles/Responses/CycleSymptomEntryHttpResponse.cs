namespace FoodDiary.Presentation.Api.Features.Cycles.Responses;

public sealed record CycleSymptomEntryHttpResponse(
    Guid Id,
    Guid CycleProfileId,
    DateTime Date,
    int Category,
    int Intensity,
    IReadOnlyCollection<string> Tags,
    string? Note);
