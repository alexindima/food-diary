namespace FoodDiary.Application.Cycles.Models;

public sealed record CyclePredictionRevisionModel(
    Guid Id,
    DateTime GeneratedAtUtc,
    DateOnly? NextPeriodStartFrom,
    DateOnly? NextPeriodStartTo,
    string Confidence,
    string DataSufficiency,
    string PatternConsistency,
    int CompletedCycleCount,
    int CalibrationSampleCount,
    double? HistoricalCoveragePercent,
    double? MeanAbsoluteErrorDays,
    IReadOnlyCollection<string> ReasonCodes,
    string AlgorithmVersion);
