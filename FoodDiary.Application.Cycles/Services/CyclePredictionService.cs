using FoodDiary.Application.Abstractions.Cycles.Models;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.Entities.Tracking;

namespace FoodDiary.Application.Cycles.Services;

public static class CyclePredictionService {
    private const int DefaultPmsWindow = 5;

    public static CyclePredictionsModel CalculatePredictions(CycleProfileReadModel profile) {
        ArgumentNullException.ThrowIfNull(profile);

        if (HasLimitedPredictionMode(profile.Mode) || HasActivePredictionLimitingFactor(profile.Factors)) {
            return new CyclePredictionsModel(
                NextPeriodStartFrom: null,
                NextPeriodStartTo: null,
                OvulationFrom: null,
                OvulationTo: null,
                PmsWindowStart: null,
                PmsWindowEnd: null,
                Confidence: profile.Confidence.ToString(),
                Rationale: "Predictions are limited by the active tracking mode.");
        }

        DateTime anchor = GetLastBleedingStart(profile.BleedingEntries) ?? profile.TrackingStartDate;
        return CalculatePredictions(
            profile.Confidence,
            profile.AverageCycleLength,
            profile.LutealLength,
            profile.ShowFertilityEstimates,
            anchor);
    }

    public static CyclePredictionsModel CalculatePredictions(CycleProfile profile) {
        ArgumentNullException.ThrowIfNull(profile);

        if (HasLimitedPredictionMode(profile.Mode) || HasActivePredictionLimitingFactor(profile.Factors)) {
            return new CyclePredictionsModel(
                NextPeriodStartFrom: null,
                NextPeriodStartTo: null,
                OvulationFrom: null,
                OvulationTo: null,
                PmsWindowStart: null,
                PmsWindowEnd: null,
                Confidence: profile.Confidence.ToString(),
                Rationale: "Predictions are limited by the active tracking mode.");
        }

        DateTime anchor = profile.GetLastBleedingStart() ?? profile.TrackingStartDate;
        return CalculatePredictions(
            profile.Confidence,
            profile.AverageCycleLength,
            profile.LutealLength,
            profile.ShowFertilityEstimates,
            anchor);
    }

    private static CyclePredictionsModel CalculatePredictions(
        CycleConfidence confidence,
        int averageCycleLength,
        int lutealLength,
        bool showFertilityEstimates,
        DateTime anchor) {
        DateTime nextPeriodStart = NormalizeDate(anchor.AddDays(averageCycleLength));
        int window = confidence switch {
            CycleConfidence.High => 1,
            CycleConfidence.Medium => 2,
            CycleConfidence.Low => 4,
            _ => 7,
        };

        DateTime nextFrom = NormalizeDate(nextPeriodStart.AddDays(-window));
        DateTime nextTo = NormalizeDate(nextPeriodStart.AddDays(window));
        DateTime ovulation = NormalizeDate(nextPeriodStart.AddDays(-lutealLength));
        DateTime ovulationFrom = NormalizeDate(ovulation.AddDays(-window));
        DateTime ovulationTo = NormalizeDate(ovulation.AddDays(window));
        DateTime pmsStart = NormalizeDate(nextPeriodStart.AddDays(-DefaultPmsWindow));

        return new CyclePredictionsModel(
            nextFrom,
            nextTo,
            showFertilityEstimates ? ovulationFrom : null,
            showFertilityEstimates ? ovulationTo : null,
            pmsStart,
            nextTo,
            confidence.ToString(),
            confidence == CycleConfidence.Learning
                ? "Learning from early cycle history; ranges are intentionally wide."
                : "Estimated from logged bleeding history and profile settings.");
    }

    private static DateTime NormalizeDate(DateTime date) =>
        DateTime.SpecifyKind(
            (date.Kind == DateTimeKind.Utc ? date : date.ToUniversalTime()).Date,
            DateTimeKind.Utc);

    private static bool HasLimitedPredictionMode(CycleTrackingMode mode) =>
        mode is CycleTrackingMode.Pregnancy or CycleTrackingMode.PostpartumLactation or CycleTrackingMode.NoPeriod;

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

    private static DateTime? GetLastBleedingStart(IEnumerable<BleedingEntryReadModel> bleedingEntries) =>
        bleedingEntries
            .Where(static entry => entry.Type == BleedingType.Bleeding)
            .OrderByDescending(static entry => entry.Date)
            .Select(static entry => (DateTime?)entry.Date)
            .FirstOrDefault();
}
