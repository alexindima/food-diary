namespace FoodDiary.Application.Abstractions.Cycles.Models;

public sealed record CyclePredictionRevisionReadModel(
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
    string ReasonCodes,
    string AlgorithmVersion);
