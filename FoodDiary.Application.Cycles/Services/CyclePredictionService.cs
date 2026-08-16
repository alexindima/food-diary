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

    public static CyclePredictionsModel CalculatePredictions(CycleProfileReadModel profile, DateTime? currentDate = null, TimeProvider? timeProvider = null) {
        ArgumentNullException.ThrowIfNull(profile);

        if (HasLimitedPredictionMode(profile.Mode) || HasActivePredictionLimitingFactor(profile.Factors)) {
            return Limited(profile.Confidence, "prediction_paused_by_state", "Predictions are paused by the active tracking state.");
        }

        return CalculatePredictions(
            profile.Confidence,
            profile.ShowFertilityEstimates,
            profile.BleedingEntries.Where(static entry => entry.Type == BleedingType.Bleeding).Select(static entry => entry.Date),
            currentDate ?? (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime);
    }

    public static CyclePredictionsModel CalculatePredictions(CycleProfile profile, DateTime? currentDate = null, TimeProvider? timeProvider = null) {
        ArgumentNullException.ThrowIfNull(profile);

        if (HasLimitedPredictionMode(profile.Mode) || HasActivePredictionLimitingFactor(profile.Factors)) {
            return Limited(profile.Confidence, "prediction_paused_by_state", "Predictions are paused by the active tracking state.");
        }

        return CalculatePredictions(
            profile.Confidence,
            profile.ShowFertilityEstimates,
            profile.BleedingEntries.Where(static entry => entry.Type == BleedingType.Bleeding).Select(static entry => entry.Date),
            currentDate ?? (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime);
    }

    private static CyclePredictionsModel CalculatePredictions(
        CycleConfidence legacyConfidence,
        bool showFertilityEstimates,
        IEnumerable<DateTime> bleedingDates,
        DateTime currentDate) {
        DateTime[] starts = BuildInferredEpisodeStarts(bleedingDates);
        int[] cycleLengths = [.. starts.Zip(starts.Skip(1), static (from, to) => (to - from).Days)];
        if (cycleLengths.Any(static length => length < MinimumReliableCycleIntervalDays)) {
            return Limited(
                legacyConfidence,
                "ambiguous_episode_history",
                "Bleeding episodes are too close together to infer reliable cycle starts.",
                dataSufficiency: "Insufficient");
        }

        if (cycleLengths.Length < MinimumCompletedCycles) {
            return Limited(
                legacyConfidence,
                "insufficient_completed_cycles",
                "At least three completed cycle intervals are needed.",
                cycleLengths.Length,
                "Insufficient");
        }

        int[] centerHistory = [.. cycleLengths.TakeLast(CenterHistoryLimit).Order()];
        int[] variabilityHistory = [.. cycleLengths.TakeLast(VariabilityHistoryLimit).Order()];
        int centerDays = Median(centerHistory);
        (int lowerDays, int upperDays) = cycleLengths.Length <= SparseHistoryMaximum
            ? (variabilityHistory[0] - SparseHistoryBufferDays, variabilityHistory[^1] + SparseHistoryBufferDays)
            : (Percentile(variabilityHistory, 0.1, roundUp: false), Percentile(variabilityHistory, 0.9, roundUp: true));
        lowerDays = Math.Min(lowerDays, centerDays - SparseHistoryBufferDays);
        upperDays = Math.Max(upperDays, centerDays + SparseHistoryBufferDays);

        DateTime anchor = starts[^1];
        DateTime nextFrom = NormalizeDate(anchor.AddDays(lowerDays));
        DateTime nextTo = NormalizeDate(anchor.AddDays(upperDays));
        DateTime today = NormalizeDate(currentDate);
        while (nextTo < today) {
            anchor = NormalizeDate(anchor.AddDays(centerDays));
            nextFrom = NormalizeDate(anchor.AddDays(lowerDays));
            nextTo = NormalizeDate(anchor.AddDays(upperDays));
        }

        int spread = variabilityHistory[^1] - variabilityHistory[0];
        string consistency = spread > LimitedConsistencySpreadDays ? "Limited" : "Consistent";
        string sufficiency = cycleLengths.Length <= SparseHistoryMaximum ? "Limited" : "Established";
        IReadOnlyCollection<string> reasonCodes = showFertilityEstimates
            ? ["estimated_from_completed_cycles", "fertility_estimate_not_available_in_v2"]
            : ["estimated_from_completed_cycles"];

        return new CyclePredictionsModel(
            nextFrom,
            nextTo,
            OvulationFrom: null,
            OvulationTo: null,
            NormalizeDate(anchor.AddDays(centerDays - DefaultPmsWindow)),
            nextTo,
            legacyConfidence.ToString(),
            "Estimated range based on recent completed cycle intervals.",
            sufficiency,
            consistency,
            cycleLengths.Length,
            reasonCodes,
            AlgorithmVersion);
    }

    private static DateTime[] BuildInferredEpisodeStarts(IEnumerable<DateTime> bleedingDates) {
        DateTime[] dates = [.. bleedingDates.Select(NormalizeDate).Distinct().Order()];
        if (dates.Length == 0) {
            return [];
        }

        var starts = new List<DateTime> { dates[0] };
        for (int index = 1; index < dates.Length; index++) {
            if ((dates[index] - dates[index - 1]).Days > 2) {
                starts.Add(dates[index]);
            }
        }

        return [.. starts];
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

    private static CyclePredictionsModel Limited(
        CycleConfidence confidence,
        string reasonCode,
        string rationale,
        int completedCycleCount = 0,
        string dataSufficiency = "Unavailable") =>
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
            [reasonCode],
            AlgorithmVersion);

    private static DateTime NormalizeDate(DateTime date) =>
        DateTime.SpecifyKind(
            (date.Kind == DateTimeKind.Utc ? date : date.ToUniversalTime()).Date,
            DateTimeKind.Utc);

    private static bool HasLimitedPredictionMode(CycleTrackingMode mode) =>
        mode is CycleTrackingMode.Pregnancy
            or CycleTrackingMode.PostpartumLactation
            or CycleTrackingMode.Perimenopause
            or CycleTrackingMode.NoPeriod;

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
