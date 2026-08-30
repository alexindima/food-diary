using System.Globalization;
using FoodDiary.Domain.Common;
using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Tracking;

public sealed class CyclePredictionRevision : Entity<CyclePredictionRevisionId> {
    private const int ClassificationMaxLength = 32;
    private const int ReasonCodesMaxLength = 512;
    private const int AlgorithmVersionMaxLength = 64;

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

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
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
        if (cycleProfileId == CycleProfileId.Empty) {
            throw new ArgumentException("Cycle profile id is required.", nameof(cycleProfileId));
        }

        if (nextPeriodStartFrom.HasValue && nextPeriodStartTo.HasValue && nextPeriodStartFrom > nextPeriodStartTo) {
            throw new ArgumentException("Prediction start date cannot be after its end date.", nameof(nextPeriodStartFrom));
        }

        if (completedCycleCount < 0) {
            throw new ArgumentOutOfRangeException(nameof(completedCycleCount), "Value must be non-negative.");
        }

        if (calibrationSampleCount < 0) {
            throw new ArgumentOutOfRangeException(nameof(calibrationSampleCount), "Value must be non-negative.");
        }

        double? normalizedCoverage = DomainGuard.NonNegativeFinite(historicalCoveragePercent, nameof(historicalCoveragePercent));
        if (normalizedCoverage > 100) {
            throw new ArgumentOutOfRangeException(nameof(historicalCoveragePercent), "Value must be in range [0, 100].");
        }

        ArgumentNullException.ThrowIfNull(reasonCodes);
        string normalizedReasonCodes = string.Join('|', reasonCodes.Select((reasonCode, index) =>
            DomainGuard.RequiredText(
                reasonCode,
                ReasonCodesMaxLength,
                string.Create(CultureInfo.InvariantCulture, $"{nameof(reasonCodes)}[{index}]"))));
        if (normalizedReasonCodes.Length > ReasonCodesMaxLength) {
            throw new ArgumentOutOfRangeException(nameof(reasonCodes), $"Combined reason codes must be at most {ReasonCodesMaxLength} characters.");
        }

        var revision = new CyclePredictionRevision(CyclePredictionRevisionId.New()) {
            CycleProfileId = cycleProfileId,
            GeneratedAtUtc = DomainGuard.RequiredUtc(generatedAtUtc, nameof(generatedAtUtc)),
            NextPeriodStartFrom = nextPeriodStartFrom,
            NextPeriodStartTo = nextPeriodStartTo,
            Confidence = DomainGuard.RequiredText(confidence, ClassificationMaxLength, nameof(confidence)),
            DataSufficiency = DomainGuard.RequiredText(dataSufficiency, ClassificationMaxLength, nameof(dataSufficiency)),
            PatternConsistency = DomainGuard.RequiredText(patternConsistency, ClassificationMaxLength, nameof(patternConsistency)),
            CompletedCycleCount = completedCycleCount,
            CalibrationSampleCount = calibrationSampleCount,
            HistoricalCoveragePercent = normalizedCoverage,
            MeanAbsoluteErrorDays = DomainGuard.NonNegativeFinite(meanAbsoluteErrorDays, nameof(meanAbsoluteErrorDays)),
            ReasonCodes = normalizedReasonCodes,
            AlgorithmVersion = DomainGuard.RequiredText(algorithmVersion, AlgorithmVersionMaxLength, nameof(algorithmVersion)),
        };
        revision.SetCreated();
        return revision;
    }
}
