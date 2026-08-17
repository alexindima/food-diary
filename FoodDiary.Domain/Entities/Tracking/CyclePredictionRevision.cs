using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Tracking;

public sealed class CyclePredictionRevision : Entity<CyclePredictionRevisionId> {
    public CycleProfileId CycleProfileId { get; private set; }
    public DateTime GeneratedAtUtc { get; private set; }
    public DateOnly? NextPeriodStartFrom { get; private set; }
    public DateOnly? NextPeriodStartTo { get; private set; }
    public string Confidence { get; private set; } = string.Empty;
    public string DataSufficiency { get; private set; } = string.Empty;
    public string PatternConsistency { get; private set; } = string.Empty;
    public int CompletedCycleCount { get; private set; }
    public int CalibrationSampleCount { get; private set; }
    public double? HistoricalCoveragePercent { get; private set; }
    public double? MeanAbsoluteErrorDays { get; private set; }
    public string ReasonCodes { get; private set; } = string.Empty;
    public string AlgorithmVersion { get; private set; } = string.Empty;

    public CycleProfile CycleProfile { get; private set; } = null!;

    private CyclePredictionRevision() {
    }

    private CyclePredictionRevision(CyclePredictionRevisionId id) : base(id) {
    }

    internal static CyclePredictionRevision Create(
        CycleProfileId cycleProfileId,
        DateTime generatedAtUtc,
        DateOnly? nextPeriodStartFrom,
        DateOnly? nextPeriodStartTo,
        string confidence,
        string dataSufficiency,
        string patternConsistency,
        int completedCycleCount,
        int calibrationSampleCount,
        double? historicalCoveragePercent,
        double? meanAbsoluteErrorDays,
        IReadOnlyCollection<string> reasonCodes,
        string algorithmVersion) {
        var revision = new CyclePredictionRevision(CyclePredictionRevisionId.New()) {
            CycleProfileId = cycleProfileId,
            GeneratedAtUtc = generatedAtUtc.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(generatedAtUtc, DateTimeKind.Utc)
                : generatedAtUtc.ToUniversalTime(),
            NextPeriodStartFrom = nextPeriodStartFrom,
            NextPeriodStartTo = nextPeriodStartTo,
            Confidence = confidence,
            DataSufficiency = dataSufficiency,
            PatternConsistency = patternConsistency,
            CompletedCycleCount = completedCycleCount,
            CalibrationSampleCount = calibrationSampleCount,
            HistoricalCoveragePercent = historicalCoveragePercent,
            MeanAbsoluteErrorDays = meanAbsoluteErrorDays,
            ReasonCodes = string.Join('|', reasonCodes),
            AlgorithmVersion = algorithmVersion,
        };
        revision.SetCreated();
        return revision;
    }
}
