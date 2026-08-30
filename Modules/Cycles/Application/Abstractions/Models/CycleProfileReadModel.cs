using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Abstractions.Cycles.Models;

public sealed record CycleProfileReadModel(
    Guid Id,
    Guid UserId,
    CycleTrackingMode Mode,
    CycleConfidence Confidence,
    DateOnly TrackingStartDate,
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
    IReadOnlyCollection<FertilitySignalReadModel> FertilitySignals,
    IReadOnlyCollection<MenstrualEpisodeReadModel>? MenstrualEpisodes = null,
    CycleTrackingGoal Goal = CycleTrackingGoal.PeriodAwareness,
    CycleReproductiveState ReproductiveState = CycleReproductiveState.Cycling,
    bool HideFromDashboard = false,
    IReadOnlyCollection<CycleConsentReadModel>? Consents = null,
    IReadOnlyCollection<CyclePredictionRevisionReadModel>? PredictionRevisions = null) {
    public bool HasActiveConsent(CycleConsentPurpose purpose) =>
        (Consents ?? []).Any(consent => consent.Purpose == purpose && consent.IsActive);
}
