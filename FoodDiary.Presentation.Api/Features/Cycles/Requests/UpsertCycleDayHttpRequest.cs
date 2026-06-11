namespace FoodDiary.Presentation.Api.Features.Cycles.Requests;

public sealed record UpsertCycleDayHttpRequest(
    DateTime Date,
    BleedingLogHttpModel? Bleeding,
    IReadOnlyCollection<SymptomLogHttpModel> Symptoms,
    FertilitySignalHttpModel? FertilitySignal);
