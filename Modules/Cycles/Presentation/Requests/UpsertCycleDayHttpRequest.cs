namespace FoodDiary.Modules.Cycles.Presentation.Requests;

public sealed record UpsertCycleDayHttpRequest(
    DateTime Date,
    BleedingLogHttpModel? Bleeding,
    IReadOnlyCollection<SymptomLogHttpModel> Symptoms,
    FertilitySignalHttpModel? FertilitySignal,
    bool ClearBleeding = false,
    IReadOnlyCollection<int>? ClearSymptomCategories = null,
    bool ClearFertilitySignal = false);
