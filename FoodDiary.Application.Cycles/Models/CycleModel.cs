using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Cycles.Models;

public sealed record CycleModel(
    Guid Id,
    Guid UserId,
    CycleTrackingMode Mode,
    CycleConfidence Confidence,
    DateTime TrackingStartDate,
    int AverageCycleLength,
    int AveragePeriodLength,
    int LutealLength,
    bool IsRegular,
    bool IsOnboardingComplete,
    bool ShowFertilityEstimates,
    bool DiscreetNotifications,
    string? Notes,
    IReadOnlyCollection<BleedingEntryModel> BleedingEntries,
    IReadOnlyCollection<CycleSymptomEntryModel> Symptoms,
    IReadOnlyCollection<CycleFactorModel> Factors,
    IReadOnlyCollection<FertilitySignalModel> FertilitySignals,
    CyclePredictionsModel? Predictions);
