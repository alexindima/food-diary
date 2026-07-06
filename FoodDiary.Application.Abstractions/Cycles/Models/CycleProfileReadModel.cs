using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Abstractions.Cycles.Models;

public sealed record CycleProfileReadModel(
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
    IReadOnlyCollection<BleedingEntryReadModel> BleedingEntries,
    IReadOnlyCollection<CycleSymptomEntryReadModel> SymptomEntries,
    IReadOnlyCollection<CycleFactorReadModel> Factors,
    IReadOnlyCollection<FertilitySignalReadModel> FertilitySignals);
