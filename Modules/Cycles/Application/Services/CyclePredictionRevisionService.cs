using FoodDiary.Modules.Cycles.Domain.ValueObjects;
using FoodDiary.Modules.Cycles.Domain.Entities;
using FoodDiary.Modules.Cycles.Contracts.Models;

namespace FoodDiary.Modules.Cycles.Application.Services;

public static class CyclePredictionRevisionService {
    public static void Record(
        CycleProfile profile,
        CyclePredictionsModel predictions,
        TimeProvider? timeProvider = null) {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(predictions);

        profile.RecordPredictionAssessment(
            (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime,
            new CyclePredictionAssessment(
                PredictionWindow.FromEndpoints(predictions.NextPeriodStartFrom, predictions.NextPeriodStartTo),
                PredictionConfidence.FromCode(predictions.Confidence),
                PredictionDataSufficiency.FromCode(predictions.DataSufficiency),
                PredictionPatternConsistency.FromCode(predictions.PatternConsistency),
                predictions.ReasonCodes.Select(PredictionReasonCode.FromCode).ToArray()),
            predictions.CompletedCycleCount,
            predictions.CalibrationSampleCount,
            predictions.HistoricalCoveragePercent,
            predictions.MeanAbsoluteErrorDays,
            predictions.AlgorithmVersion);
    }
}
