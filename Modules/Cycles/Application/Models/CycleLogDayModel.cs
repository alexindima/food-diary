namespace FoodDiary.Application.Cycles.Models;

public sealed record CycleLogDayModel(
    Guid CycleProfileId,
    DateOnly Date,
    IReadOnlyCollection<BleedingEntryModel> BleedingEntries,
    IReadOnlyCollection<CycleSymptomEntryModel> Symptoms,
    FertilitySignalModel? FertilitySignal);
