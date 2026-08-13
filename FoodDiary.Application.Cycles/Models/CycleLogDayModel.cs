namespace FoodDiary.Application.Cycles.Models;

public sealed record CycleLogDayModel(
    Guid CycleProfileId,
    DateTime Date,
    IReadOnlyCollection<BleedingEntryModel> BleedingEntries,
    IReadOnlyCollection<CycleSymptomEntryModel> Symptoms,
    FertilitySignalModel? FertilitySignal);
