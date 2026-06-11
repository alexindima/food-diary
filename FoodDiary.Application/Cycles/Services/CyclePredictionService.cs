using FoodDiary.Application.Cycles.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.Entities.Tracking;

namespace FoodDiary.Application.Cycles.Services;

public static class CyclePredictionService {
    private const int DefaultPmsWindow = 5;

    public static CyclePredictionsModel CalculatePredictions(CycleProfile profile) {
        ArgumentNullException.ThrowIfNull(profile);

        if (profile.Mode is CycleTrackingMode.Pregnancy or CycleTrackingMode.PostpartumLactation or CycleTrackingMode.NoPeriod) {
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
        DateTime nextPeriodStart = NormalizeDate(anchor.AddDays(profile.AverageCycleLength));
        int window = profile.Confidence switch {
            CycleConfidence.High => 1,
            CycleConfidence.Medium => 2,
            CycleConfidence.Low => 4,
            _ => 7,
        };

        DateTime nextFrom = NormalizeDate(nextPeriodStart.AddDays(-window));
        DateTime nextTo = NormalizeDate(nextPeriodStart.AddDays(window));
        DateTime ovulation = NormalizeDate(nextPeriodStart.AddDays(-profile.LutealLength));
        DateTime ovulationFrom = NormalizeDate(ovulation.AddDays(-window));
        DateTime ovulationTo = NormalizeDate(ovulation.AddDays(window));
        DateTime pmsStart = NormalizeDate(nextPeriodStart.AddDays(-DefaultPmsWindow));

        return new CyclePredictionsModel(
            nextFrom,
            nextTo,
            profile.ShowFertilityEstimates ? ovulationFrom : null,
            profile.ShowFertilityEstimates ? ovulationTo : null,
            pmsStart,
            nextTo,
            profile.Confidence.ToString(),
            profile.Confidence == CycleConfidence.Learning
                ? "Learning from early cycle history; ranges are intentionally wide."
                : "Estimated from logged bleeding history and profile settings.");
    }

    private static DateTime NormalizeDate(DateTime date) =>
        DateTime.SpecifyKind(
            (date.Kind == DateTimeKind.Utc ? date : date.ToUniversalTime()).Date,
            DateTimeKind.Utc);
}
