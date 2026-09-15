namespace FoodDiary.Modules.Cycles.Presentation.Contracts.Responses;

public sealed record CycleSymptomEntryHttpResponse(
    Guid Id,
    Guid CycleProfileId,
    DateTime Date,
    int Category,
    int Intensity,
    IReadOnlyCollection<string> Tags,
    string? Note);
