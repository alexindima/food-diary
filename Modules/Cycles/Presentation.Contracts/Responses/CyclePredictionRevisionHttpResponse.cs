namespace FoodDiary.Modules.Cycles.Presentation.Contracts.Responses;

public sealed record CyclePredictionRevisionHttpResponse(
    Guid Id,
    DateTime GeneratedAtUtc,
    DateTime? NextPeriodStartFrom,
    DateTime? NextPeriodStartTo,
    string Confidence,
    string DataSufficiency,
    string PatternConsistency,
    int CompletedCycleCount,
    int CalibrationSampleCount,
    double? HistoricalCoveragePercent,
    double? MeanAbsoluteErrorDays,
    IReadOnlyCollection<string> ReasonCodes,
    string AlgorithmVersion);
