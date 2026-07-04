using FoodDiary.Application.Fasting.Models;
using FoodDiary.Domain.Entities.Tracking.Fasting;

namespace FoodDiary.Application.Fasting.Services;

internal static class FastingAlertBuilder {
    public static IReadOnlyList<FastingMessageModel> Build(
        FastingOccurrence? current,
        FastingCheckInSnapshot? latestCheckIn,
        DateTime nowUtc) {
        var alerts = new List<FastingMessageModel>();

        FastingMessageModel? currentWarning = BuildCurrentWarning(current, latestCheckIn);
        if (currentWarning is not null) {
            alerts.Add(currentWarning);
        }

        if (current?.EndedAtUtc.HasValue != false) {
            return alerts;
        }

        if (HasRiskyCurrentCheckIn(current, latestCheckIn)) {
            alerts.Add(new FastingMessageModel(
                "risky",
                "FASTING.PROMPTS.RISKY_TITLE",
                "FASTING.PROMPTS.RISKY_BODY",
                "warning"));
            return alerts;
        }

        if (latestCheckIn is not null) {
            return alerts;
        }

        switch ((nowUtc - current.StartedAtUtc).TotalHours) {
            case >= 20:
                alerts.Add(new FastingMessageModel(
                    "late",
                    "FASTING.PROMPTS.LATE_TITLE",
                    "FASTING.PROMPTS.LATE_BODY",
                    "warning"));
                break;
            case >= 12:
                alerts.Add(new FastingMessageModel(
                    "mid",
                    "FASTING.PROMPTS.MID_TITLE",
                    "FASTING.PROMPTS.MID_BODY",
                    "neutral"));
                break;
        }

        return alerts;
    }

    private static FastingMessageModel? BuildCurrentWarning(FastingOccurrence? current, FastingCheckInSnapshot? latestCheckIn) {
        if (current is null || !HasRiskyCurrentCheckIn(current, latestCheckIn)) {
            return null;
        }

        return new FastingMessageModel(
            "current-warning",
            "FASTING.INSIGHTS.CURRENT_WARNING_TITLE",
            "FASTING.INSIGHTS.CURRENT_WARNING_BODY",
            "warning");
    }

    private static bool HasRiskyCurrentCheckIn(FastingOccurrence occurrence, FastingCheckInSnapshot? latestCheckIn) {
        if (latestCheckIn is null) {
            return false;
        }

        bool lowEnergy = latestCheckIn.EnergyLevel <= 2;
        bool lowMood = latestCheckIn.MoodLevel <= 2;
        bool hasRiskySymptom = latestCheckIn.Symptoms.Any(FastingSymptomCatalog.RiskySymptoms.Contains);

        return occurrence.EndedAtUtc is null && hasRiskySymptom && (lowEnergy || lowMood);
    }
}
