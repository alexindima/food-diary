using FoodDiary.Application.Abstractions.Cycles.Models;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Cycles.Services;

public static class CyclePredictionService {
    private const string AlgorithmVersion = "period-v2.0";
    private const int DefaultPmsWindow = 5;
    private const int MinimumCompletedCycles = 3;
    private const int SparseHistoryMaximum = 5;
    private const int CenterHistoryLimit = 6;
    private const int VariabilityHistoryLimit = 12;
    private const int SparseHistoryBufferDays = 2;
    private const int LimitedConsistencySpreadDays = 9;
    private const int MinimumReliableCycleIntervalDays = 15;

    public static CyclePredictionsModel CalculatePredictions(CycleProfileReadModel profile, DateOnly? currentDate = null, TimeProvider? timeProvider = null) {
        ArgumentNullException.ThrowIfNull(profile);

        if (HasLimitedPredictionState(profile.ReproductiveState) || HasActivePredictionLimitingFactor(profile.Factors)) {
            return Limited(profile.Confidence, "prediction_paused_by_state", "Predictions are paused by the active tracking state.");
        }

        return CalculatePredictions(
            profile.Confidence,
            profile.ShowFertilityEstimates,
            ResolvePredictionHistory(profile.BleedingEntries, profile.MenstrualEpisodes ?? []),
            currentDate ?? DateOnly.FromDateTime((timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime));
    }

    public static CyclePredictionsModel CalculatePredictions(CycleProfile profile, DateOnly? currentDate = null, TimeProvider? timeProvider = null) {
        ArgumentNullException.ThrowIfNull(profile);

        if (HasLimitedPredictionState(profile.ReproductiveState) || HasActivePredictionLimitingFactor(profile.Factors)) {
            return Limited(profile.Confidence, "prediction_paused_by_state", "Predictions are paused by the active tracking state.");
        }

        return CalculatePredictions(
            profile.Confidence,
            profile.ShowFertilityEstimates,
            ResolvePredictionHistory(profile),
            currentDate ?? DateOnly.FromDateTime((timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime));
    }

    private static CyclePredictionsModel CalculatePredictions(
        CycleConfidence legacyConfidence,
        bool showFertilityEstimates,
        PredictionHistory history,
        DateOnly currentDate) {
        DateOnly[] starts = history.UsedEpisodeStarts;
        int[] cycleLengths = [.. starts.Zip(starts.Skip(1), static (from, to) => to.DayNumber - from.DayNumber)];
        if (cycleLengths.Any(static length => length < MinimumReliableCycleIntervalDays)) {
            return Limited(
                legacyConfidence,
                "ambiguous_episode_history",
                "Bleeding episodes are too close together to infer reliable cycle starts.",
                dataSufficiency: "Insufficient",
                usedEpisodeCount: history.UsedEpisodeStarts.Length,
                excludedEpisodeCount: history.ExcludedEpisodeCount);
        }

        if (cycleLengths.Length < MinimumCompletedCycles) {
            return Limited(
                legacyConfidence,
                "insufficient_completed_cycles",
                "At least three completed cycle intervals are needed.",
                cycleLengths.Length,
                "Insufficient",
                history.UsedEpisodeStarts.Length,
                history.ExcludedEpisodeCount);
        }

        int[] centerHistory = [.. cycleLengths.TakeLast(CenterHistoryLimit).Order()];
        int[] variabilityHistory = [.. cycleLengths.TakeLast(VariabilityHistoryLimit).Order()];
        int centerDays = Median(centerHistory);
        (int lowerDays, int upperDays) = cycleLengths.Length <= SparseHistoryMaximum
            ? (variabilityHistory[0] - SparseHistoryBufferDays, variabilityHistory[^1] + SparseHistoryBufferDays)
            : (Percentile(variabilityHistory, 0.1, roundUp: false), Percentile(variabilityHistory, 0.9, roundUp: true));
        lowerDays = Math.Min(lowerDays, centerDays - SparseHistoryBufferDays);
        upperDays = Math.Max(upperDays, centerDays + SparseHistoryBufferDays);

        DateOnly anchor = starts[^1];
        DateOnly nextFrom = anchor.AddDays(lowerDays);
        DateOnly nextTo = anchor.AddDays(upperDays);
        DateOnly today = currentDate;
        while (nextTo < today) {
            anchor = anchor.AddDays(centerDays);
            nextFrom = anchor.AddDays(lowerDays);
            nextTo = anchor.AddDays(upperDays);
        }

        int spread = variabilityHistory[^1] - variabilityHistory[0];
        string consistency = spread > LimitedConsistencySpreadDays ? "Limited" : "Consistent";
        string sufficiency = cycleLengths.Length <= SparseHistoryMaximum ? "Limited" : "Established";
        IReadOnlyCollection<string> reasonCodes = showFertilityEstimates
            ? ["estimated_from_completed_cycles", "fertility_estimate_not_available_in_v2"]
            : ["estimated_from_completed_cycles"];
        CalibrationMetrics calibration = CalculateCalibration(cycleLengths);

        return new CyclePredictionsModel(
            nextFrom,
            nextTo,
            OvulationFrom: null,
            OvulationTo: null,
            anchor.AddDays(centerDays - DefaultPmsWindow),
            nextTo,
            legacyConfidence.ToString(),
            "Estimated range based on recent completed cycle intervals.",
            sufficiency,
            consistency,
            cycleLengths.Length,
            history.UsedEpisodeStarts.Length,
            history.ExcludedEpisodeCount,
            reasonCodes,
            AlgorithmVersion,
            calibration.SampleCount,
            calibration.CoveragePercent,
            calibration.MeanAbsoluteErrorDays);
    }

    private static DateOnly[] BuildInferredEpisodeStarts(IEnumerable<DateOnly> bleedingDates) {
        DateOnly[] dates = [.. bleedingDates.Distinct().Order()];
        if (dates.Length == 0) {
            return [];
        }

        var starts = new List<DateOnly> { dates[0] };
        for (int index = 1; index < dates.Length; index++) {
            if (dates[index].DayNumber - dates[index - 1].DayNumber > 2) {
                starts.Add(dates[index]);
            }
        }

        return [.. starts];
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    private static PredictionHistory ResolvePredictionHistory(
        IEnumerable<BleedingEntryReadModel> bleedingEntries,
        IEnumerable<MenstrualEpisodeReadModel> persistedEpisodes) {
        MenstrualEpisodeReadModel[] episodes = [.. persistedEpisodes];
        DateOnly[] confirmed = [.. episodes
            .Where(static episode => episode.Status == MenstrualEpisodeStatus.Confirmed && !episode.ExcludedFromPredictions)
            .Select(static episode => episode.StartDate)];
        DateOnly[] excluded = [.. episodes
            .Where(static episode => episode.Status == MenstrualEpisodeStatus.Confirmed && episode.ExcludedFromPredictions)
            .Select(static episode => episode.StartDate)];
        DateOnly[] inferred = BuildInferredEpisodeStarts(
            bleedingEntries.Where(static entry => entry.Type == BleedingType.Bleeding).Select(static entry => entry.Date));
        return CreatePredictionHistory(confirmed, excluded, inferred);
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    private static PredictionHistory ResolvePredictionHistory(CycleProfile profile) {
        DateOnly[] confirmed = [.. profile.MenstrualEpisodes
            .Where(static episode => episode.Status == MenstrualEpisodeStatus.Confirmed && !episode.ExcludedFromPredictions)
            .Select(static episode => episode.StartDate)];
        DateOnly[] excluded = [.. profile.MenstrualEpisodes
            .Where(static episode => episode.Status == MenstrualEpisodeStatus.Confirmed && episode.ExcludedFromPredictions)
            .Select(static episode => episode.StartDate)];
        DateOnly[] inferred = BuildInferredEpisodeStarts(
            profile.BleedingEntries.Where(static entry => entry.Type == BleedingType.Bleeding).Select(static entry => entry.Date));
        return CreatePredictionHistory(confirmed, excluded, inferred);
    }

    private static PredictionHistory CreatePredictionHistory(
        IEnumerable<DateOnly> confirmedStarts,
        IEnumerable<DateOnly> excludedStarts,
        IEnumerable<DateOnly> inferredStarts) {
        DateOnly[] excluded = [.. excludedStarts.Distinct()];
        DateOnly[] eligibleInferred = [.. inferredStarts.Where(inferred =>
            !excluded.Any(excludedStart => Math.Abs(excludedStart.DayNumber - inferred.DayNumber) <= 2))];
        return new PredictionHistory(
            MergeEpisodeStarts(confirmedStarts, eligibleInferred),
            excluded.Length);
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    private static DateOnly[] MergeEpisodeStarts(IEnumerable<DateOnly> confirmedStarts, IEnumerable<DateOnly> inferredStarts) {
        DateOnly[] confirmed = [.. confirmedStarts.Distinct()];
        return [.. confirmed
            .Concat(inferredStarts.Where(inferred => !confirmed.Any(confirmedStart => Math.Abs(confirmedStart.DayNumber - inferred.DayNumber) <= 2)))
            .Distinct()
            .Order()];
    }

    private static int Median(IReadOnlyList<int> sortedValues) {
        int middle = sortedValues.Count / 2;
        return sortedValues.Count % 2 == 0
            ? (int)Math.Round((sortedValues[middle - 1] + sortedValues[middle]) / 2d, MidpointRounding.AwayFromZero)
            : sortedValues[middle];
    }

    private static int Percentile(IReadOnlyList<int> sortedValues, double percentile, bool roundUp) {
        double index = (sortedValues.Count - 1) * percentile;
        int lowerIndex = (int)Math.Floor(index);
        int upperIndex = (int)Math.Ceiling(index);
        double value = sortedValues[lowerIndex] + ((index - lowerIndex) * (sortedValues[upperIndex] - sortedValues[lowerIndex]));
        return roundUp ? (int)Math.Ceiling(value) : (int)Math.Floor(value);
    }

    private static CalibrationMetrics CalculateCalibration(IReadOnlyList<int> cycleLengths) {
        if (cycleLengths.Count <= MinimumCompletedCycles) {
            return new CalibrationMetrics(
                SampleCount: 0,
                CoveragePercent: null,
                MeanAbsoluteErrorDays: null);
        }

        var errors = new List<int>();
        int covered = 0;
        for (int targetIndex = MinimumCompletedCycles; targetIndex < cycleLengths.Count; targetIndex++) {
            int[] prior = [.. cycleLengths.Take(targetIndex).TakeLast(VariabilityHistoryLimit).Order()];
            int predicted = Median(prior);
            int actual = cycleLengths[targetIndex];
            errors.Add(Math.Abs(actual - predicted));
            if (actual >= prior[0] - SparseHistoryBufferDays && actual <= prior[^1] + SparseHistoryBufferDays) {
                covered++;
            }
        }

        return new CalibrationMetrics(
            errors.Count,
            Math.Round(covered * 100d / errors.Count, 1, MidpointRounding.AwayFromZero),
            Math.Round(errors.Average(), 1, MidpointRounding.AwayFromZero));
    }

    private static CyclePredictionsModel Limited(
        CycleConfidence confidence,
        string reasonCode,
        string rationale,
        int completedCycleCount = 0,
        string dataSufficiency = "Unavailable",
        int usedEpisodeCount = 0,
        int excludedEpisodeCount = 0) =>
        new(
            NextPeriodStartFrom: null,
            NextPeriodStartTo: null,
            OvulationFrom: null,
            OvulationTo: null,
            PmsWindowStart: null,
            PmsWindowEnd: null,
            confidence.ToString(),
            rationale,
            dataSufficiency,
            "Unavailable",
            completedCycleCount,
            usedEpisodeCount,
            excludedEpisodeCount,
            [reasonCode],
            AlgorithmVersion);

    private sealed record PredictionHistory(DateOnly[] UsedEpisodeStarts, int ExcludedEpisodeCount);

    private sealed record CalibrationMetrics(
        int SampleCount,
        double? CoveragePercent,
        double? MeanAbsoluteErrorDays);

    private static bool HasLimitedPredictionState(CycleReproductiveState state) =>
        state is CycleReproductiveState.Pregnancy
            or CycleReproductiveState.Postpartum
            or CycleReproductiveState.Lactation
            or CycleReproductiveState.Perimenopause
            or CycleReproductiveState.NoPeriod;

    private static bool HasActivePredictionLimitingFactor(IEnumerable<CycleFactor> factors) =>
        factors.Any(factor =>
            factor.EndDate is null &&
            factor.Type is CycleFactorType.Pregnancy
                or CycleFactorType.Lactation
                or CycleFactorType.HormonalContraception
                or CycleFactorType.Postpartum
                or CycleFactorType.NoPeriod);

    private static bool HasActivePredictionLimitingFactor(IEnumerable<CycleFactorReadModel> factors) =>
        factors.Any(factor =>
            factor.EndDate is null &&
            factor.Type is CycleFactorType.Pregnancy
                or CycleFactorType.Lactation
                or CycleFactorType.HormonalContraception
                or CycleFactorType.Postpartum
                or CycleFactorType.NoPeriod);
}
