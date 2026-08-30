namespace FoodDiary.Application.Cycles.Models;

public sealed record CyclePredictionsModel(
    DateOnly? NextPeriodStartFrom,
    DateOnly? NextPeriodStartTo,
    DateOnly? OvulationFrom,
    DateOnly? OvulationTo,
    DateOnly? PmsWindowStart,
    DateOnly? PmsWindowEnd,
    string Confidence,
    string Rationale,
    string DataSufficiency,
    string PatternConsistency,
    int CompletedCycleCount,
    int UsedEpisodeCount,
    int ExcludedEpisodeCount,
    IReadOnlyCollection<string> ReasonCodes,
    string AlgorithmVersion,
    int CalibrationSampleCount = 0,
    double? HistoricalCoveragePercent = null,
    double? MeanAbsoluteErrorDays = null);
