namespace FoodDiary.Presentation.Api.Features.Cycles.Responses;

public sealed record CycleLogDayHttpResponse(
    Guid CycleProfileId,
    DateTime Date,
    IReadOnlyCollection<BleedingEntryHttpResponse> BleedingEntries,
    IReadOnlyCollection<CycleSymptomEntryHttpResponse> Symptoms,
    FertilitySignalHttpResponse? FertilitySignal);
