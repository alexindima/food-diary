using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Cycles.Models;

public sealed record CycleModel(
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
    IReadOnlyCollection<BleedingEntryModel> BleedingEntries,
    IReadOnlyCollection<CycleSymptomEntryModel> Symptoms,
    IReadOnlyCollection<CycleFactorModel> Factors,
    IReadOnlyCollection<FertilitySignalModel> FertilitySignals,
    CyclePredictionsModel? Predictions,
    IReadOnlyCollection<MenstrualEpisodeModel>? MenstrualEpisodes = null,
    CycleTrackingGoal Goal = CycleTrackingGoal.PeriodAwareness,
    CycleReproductiveState ReproductiveState = CycleReproductiveState.Cycling,
    bool HideFromDashboard = false,
    IReadOnlyCollection<CycleConsentModel>? Consents = null,
    IReadOnlyCollection<CyclePredictionRevisionModel>? PredictionRevisions = null);
