namespace FoodDiary.Presentation.Api.Features.Cycles.Responses;

public sealed record CycleHttpResponse(
    Guid Id,
    Guid UserId,
    int Mode,
    int Confidence,
    DateTime TrackingStartDate,
    int AverageCycleLength,
    int AveragePeriodLength,
    int LutealLength,
    bool IsRegular,
    bool IsOnboardingComplete,
    bool ShowFertilityEstimates,
    bool DiscreetNotifications,
    string? Notes,
    IReadOnlyCollection<BleedingEntryHttpResponse> BleedingEntries,
    IReadOnlyCollection<CycleSymptomEntryHttpResponse> Symptoms,
    IReadOnlyCollection<CycleFactorHttpResponse> Factors,
    IReadOnlyCollection<FertilitySignalHttpResponse> FertilitySignals,
    CyclePredictionsHttpResponse? Predictions);
