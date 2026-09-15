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

        profile.RecordPredictionRevision(
            (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime,
            predictions.NextPeriodStartFrom,
            predictions.NextPeriodStartTo,
            predictions.Confidence,
            predictions.DataSufficiency,
            predictions.PatternConsistency,
            predictions.CompletedCycleCount,
            predictions.CalibrationSampleCount,
            predictions.HistoricalCoveragePercent,
            predictions.MeanAbsoluteErrorDays,
            predictions.ReasonCodes,
            predictions.AlgorithmVersion);
    }
}
