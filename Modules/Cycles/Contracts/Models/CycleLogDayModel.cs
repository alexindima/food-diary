namespace FoodDiary.Modules.Cycles.Contracts.Models;

public sealed record CycleLogDayModel(
    Guid CycleProfileId,
    DateOnly Date,
    IReadOnlyCollection<BleedingEntryModel> BleedingEntries,
    IReadOnlyCollection<CycleSymptomEntryModel> Symptoms,
    FertilitySignalModel? FertilitySignal);
