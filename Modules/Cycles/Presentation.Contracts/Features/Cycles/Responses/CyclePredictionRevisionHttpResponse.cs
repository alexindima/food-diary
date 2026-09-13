namespace FoodDiary.Presentation.Api.Features.Cycles.Responses;

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
